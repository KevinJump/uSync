using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.ContentEdition.Serializers;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Extensions;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Handlers
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
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<TObject> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
        { }

        [Obsolete("Use constructors with collections")]
        protected ContentHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, appCaches, syncFileService)
        { }

        [Obsolete("Use constructors with collections")]
        protected ContentHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            ISyncDependencyChecker<TObject> checker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, appCaches, checker, syncFileService)
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
                    logger.Debug(handlerType, "Not processing item, {0} path {1} not in include path", node.Attribute("Alias").ValueOrDefault("unknown"), path);
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
                    logger.Debug(handlerType, "Not processing item, {0} path {1} is excluded", node.Attribute("Alias").ValueOrDefault("unknown"), path);
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

        /// <summary>
        /// </summary>
        /// <remarks>
        ///  content items sit in a tree, so we don't want to give them
        ///  an alias based on their name (because of false matches)
        ///  so we give the key as an alias.
        /// </remarks>
        /// <param name="item"></param>
        /// <returns></returns>
        protected override string GetItemAlias(TObject item)
            => item.Key.ToString();


        public virtual IEnumerable<uSyncAction> ProcessCleanActions(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
        {
            var cleans = actions.Where(x => x.Change == ChangeType.Clean && !string.IsNullOrWhiteSpace(x.FileName)).ToList();
            if (cleans.Count == 0) return Enumerable.Empty<uSyncAction>();

            var results = new List<uSyncAction>();

            foreach(var clean in cleans)
            {
                if (!string.IsNullOrWhiteSpace(clean.FileName))
                    results.AddRange(CleanFolder(clean.FileName, false, config.UseFlatStructure));
            }

            return results;
        }


        protected override bool DoItemsMatch(XElement node, TObject item)
        {
            if (node.GetKey() == GetItemKey(item)) return true;

            if (this.serializer is ContentSerializerBase<TObject> baseSerializer)
            {
                var itemPath = baseSerializer.GetItemPath(item);
                var nodePath = node.Element("Info")?.Element("Path").ValueOrDefault(string.Empty);
                if (itemPath == nodePath) return true;
            }

            return false;
        }

        /// <summary>
        ///  for content, we could have 100,000s of files and reading them all would be slow. 
        /// </summary>
        protected override void CleanUp(TObject item, string newFile, string folder)
        {
            // for content this clean up check only catches when an item is moved from
            // one location to another, if the site is setup to useGuidNames and a flat 
            // structure that rename won't actually leave any old files on disk. 

            bool quickCleanup = this.DefaultConfig.GetSetting("QuickCleanup", false);
            if (quickCleanup)
            {
                logger.Debug(handlerType, "Quick cleanup is on, so not looking in all config files");
                return;
            }

            // so we can skip this step and get a much quicker save process.
            if (this.DefaultConfig.GuidNames && this.DefaultConfig.UseFlatStructure) return;
            base.CleanUp(item, newFile, folder);
        }
    }

}
