﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Serialization;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    /// <summary>
    ///  base for all content based handlers
    /// </summary>
    /// <remarks>
    ///  Content based handlers can have the same name in different 
    ///  places around the tree, so we have to check for file name
    ///  clashes. 
    /// </remarks>
    public abstract class ContentHandlerBase<TObject, TService> : SyncHandlerTreeBase<TObject, TService>
        where TObject : IContentBase
        where TService : IService
    {

        protected ContentHandlerBase(
            ILogger<ContentHandlerBase<TObject, TService>> logger,
            IEntityService entityService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfigService,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
        { }

        protected override string GetItemMatchString(TObject item)
        {
            var itemPath = item.Level.ToString();
            if (item.Trashed && serializer is ISyncContentSerializer<TObject> contentSerializer)
            {
                itemPath = contentSerializer.GetItemPath(item);
            }
            return $"{item.Name}_{itemPath}".ToLower();
        }

        protected override string GetXmlMatchString(XElement node)
        {
            var path = node.Element("Info")?.Element("Path").ValueOrDefault(node.GetLevel().ToString());
            return $"{node.GetAlias()}_{path}".ToLower();
        }


        /*
         *  Config options. 
         *    Include = Paths (comma seperated) (only include if path starts with one of these)
         *    Exclude = Paths (comma seperated) (exclude if path starts with one of these)
         *    
         *    RulesOnExport = bool (do we apply the rules on export as well as import?)
         */

        protected override bool ShouldImport(XElement node, HandlerSettings config)
        {
            // check base first - if it says no - then no point checking this. 
            if (!base.ShouldImport(node, config)) return false;

            if (!ImportTrashedItem(node, config)) return false;

            if (!ImportPaths(node, config)) return false;

            return true;
        }

        private bool ImportTrashedItem(XElement node, HandlerSettings config)
        {
            // unless the setting is explicit we don't import trashed items. 
            var trashed = node.Element("Info")?.Element("Trashed").ValueOrDefault(false);
            if (trashed.GetValueOrDefault(false) && !config.GetSetting("ImportTrashed", true)) return false;

            return true;
        }

        private bool ImportPaths(XElement node, HandlerSettings config)
        {
            var include = config.GetSetting("Include", "")
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (include.Length > 0)
            {
                var path = node.Element("Info")?.Element("Path").ValueOrDefault(string.Empty);
                if (!string.IsNullOrWhiteSpace(path) && !include.Any(x => path.InvariantStartsWith(x)))
                {
                    logger.LogDebug("Not processing item, {0} path {1} not in include path", node.Attribute("Alias").ValueOrDefault("unknown"), path);
                    return false;
                }
            }

            var exclude = config.GetSetting("Exclude", "")
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (exclude.Length > 0)
            {
                var path = node.Element("Info")?.Element("Path").ValueOrDefault(string.Empty);
                if (!string.IsNullOrWhiteSpace(path) && exclude.Any(x => path.InvariantStartsWith(x)))
                {
                    logger.LogDebug("Not processing item, {0} path {1} is excluded", node.Attribute("Alias").ValueOrDefault("unknown"), path);
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        ///  Should we save this value to disk?
        /// </summary>
        /// <remarks>
        ///  In general we save everything to disk, even if we are not going to remimport it later
        ///  but you can stop this with RulesOnExport = true in the settings 
        /// </remarks>

        protected override bool ShouldExport(XElement node, HandlerSettings config)
        {
            // We export trashed items by default, (but we don't import them by default)
            var trashed = node.Element("Info")?.Element("Trashed").ValueOrDefault(false);
            if (trashed.GetValueOrDefault(false) && !config.GetSetting<bool>("ExportTrashed", true)) return false;

            if (config.GetSetting("RulesOnExport", false))
            {
                // we run the import rules (but not the base rules as that would confuse.)
                if (!ImportTrashedItem(node, config)) return false;
                if (!ImportPaths(node, config)) return false;
            }

            return true;
        }


        // we only match duplicate actions by key. 
        protected override bool DoActionsMatch(uSyncAction a, uSyncAction b)
            => a.key == b.key;


        public virtual IEnumerable<uSyncAction> ProcessCleanActions(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
        {
            var cleans = actions.Where(x => x.Change == ChangeType.Clean && !string.IsNullOrWhiteSpace(x.FileName)).ToList();
            if (cleans.Count == 0) return Enumerable.Empty<uSyncAction>();

            var results = new List<uSyncAction>();

            foreach (var clean in cleans)
            {
                if (!string.IsNullOrWhiteSpace(clean.FileName))
                    results.AddRange(CleanFolder(clean.FileName, false, config.UseFlatStructure));
            }

            return results;
        }

        public void Handle(MovedToRecycleBinNotification<TObject> notification)
        {
            if (!ShouldProcessEvent()) return;
            HandleMove(notification.MoveInfoCollection);
        }


        protected override void CleanUp(TObject item, string newFile, string folder)
        {
            // for content this clean up check only catches when an item is moved from
            // one location to another, if the site is setup to useGuidNames and a flat 
            // structure that rename won't actually leave any old files on disk. 

            bool quickCleanup = this.DefaultConfig.GetSetting("QuickCleanup", false);
            if (quickCleanup)
            {
                logger.LogDebug("Quick cleanup is on, so not looking in all config files");
                return;
            }

            // so we can skip this step and get a much quicker save process.
            if (this.DefaultConfig.GuidNames && this.DefaultConfig.UseFlatStructure) return;

            // check to see if we think this was a rename (so only do the clean up if we really have to)
            if (item.WasPropertyDirty(nameof(item.Name)) || item.WasPropertyDirty(nameof(item.ParentId)))
            {
                base.CleanUp(item, newFile, folder);
            }
        }

    }

}
