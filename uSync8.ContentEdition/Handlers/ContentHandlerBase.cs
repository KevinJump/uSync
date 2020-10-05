using System;
using System.IO;
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

            // unless the setting is explicit we don't import trashed items. 
            var trashed = node.Element("Info")?.Element("Trashed").ValueOrDefault(false);
            if (trashed.GetValueOrDefault(false) && !config.GetSetting("ImportTrashed", true)) return false;

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
            if (trashed.GetValueOrDefault(false) && config.GetSetting<bool>("ExportTrashed", true)) return false;

            if (config.GetSetting("RulesOnExport", false))
            {
                return ShouldImport(node, config);
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
    }

}
