using System;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync8.ContentEdition.Handlers
{
    [SyncHandler("mediaHandler", "Media", "Media", uSyncBackOfficeConstants.Priorites.Media,
        Icon = "icon-picture usync-addon-icon", IsTwoPass = true, EntityType = UdiEntityType.Media)]
    public class MediaHandler : SyncHandlerTreeBase<IMedia, IMediaService>, ISyncHandler, ISyncExtendedHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IMediaService mediaService;

        private readonly bool performDoubleLookup;

        public MediaHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            IMediaService mediaService,
            ISyncSerializer<IMedia> serializer,
            ISyncTracker<IMedia> tracker,
            ISyncDependencyChecker<IMedia> checker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, checker, syncFileService)
        {
            this.mediaService = mediaService;
            performDoubleLookup = UmbracoVersion.LocalVersion.Major != 8 || UmbracoVersion.LocalVersion.Minor < 4;

        }

        protected override void DeleteViaService(IMedia item)
            => mediaService.Delete(item);

        protected override IMedia GetFromService(int id)
            => mediaService.GetById(id);

        protected override IMedia GetFromService(Guid key)
        {
            if (performDoubleLookup)
            {
                // fixed v8.4+ by https://github.com/umbraco/Umbraco-CMS/issues/2997
                var entity = entityService.Get(key);
                if (entity != null)
                    return mediaService.GetById(entity.Id);
            }
            else
            {
                return mediaService.GetById(key);
            }

            return null;
        }


        protected override IMedia GetFromService(string alias)
            => null;

        protected override void InitializeEvents(HandlerSettings settings)
        {
            MediaService.Saved += EventSavedItem;
            MediaService.Deleted += EventDeletedItem;
            MediaService.Moved += EventMovedItem;
            MediaService.Trashed += EventMovedItem;
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
            if (config.Settings.ContainsKey("Include"))
            {
                var include = config.Settings["Include"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (include.Length > 0)
                {
                    var path = node.Element("Info")?.Element("Path").ValueOrDefault(string.Empty);
                    if (!string.IsNullOrWhiteSpace(path) && !include.Any(x => path.InvariantStartsWith(x)))
                    {
                        logger.Debug<ContentHandler>("Not processing item, {0} path {1} not in include path", node.Attribute("Alias").ValueOrDefault("unknown"), path);
                        return false;
                    }
                }
            }

            if (config.Settings.ContainsKey("Exclude"))
            {
                var exclude = config.Settings["Exclude"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (exclude.Length > 0)
                {
                    var path = node.Element("Info")?.Element("Path").ValueOrDefault(string.Empty);
                    if (!string.IsNullOrWhiteSpace(path) && exclude.Any(x => path.InvariantStartsWith(x)))
                    {
                        logger.Debug<ContentHandler>("Not processing item, {0} path {1} is excluded", node.Attribute("Alias").ValueOrDefault("unknown"), path);
                        return false;
                    }
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
            if (config.Settings.ContainsKey("RulesOnExport") && config.Settings["RulesOnExport"].InvariantEquals("true"))
            {
                return ShouldImport(node, config);
            }

            return true;
        }

    }
}
