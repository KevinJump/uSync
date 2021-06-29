using System;
using System.Collections.Generic;

using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync8.ContentEdition.Handlers
{
    [SyncHandler("mediaHandler", "Media", "Media", uSyncBackOfficeConstants.Priorites.Media,
        Icon = "icon-picture usync-addon-icon", IsTwoPass = true, EntityType = UdiEntityType.Media)]
    public class MediaHandler : ContentHandlerBase<IMedia, IMediaService>, ISyncHandler, ISyncExtendedHandler, ISyncItemHandler, ISyncCleanEntryHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IMediaService mediaService;

        private readonly bool performDoubleLookup;

        public MediaHandler(
            IMediaService mediaService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IMedia> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
        {
            this.mediaService = mediaService;
            performDoubleLookup = UmbracoVersion.LocalVersion.Major != 8 || UmbracoVersion.LocalVersion.Minor < 4;

        }

        [Obsolete("Use constructors with collections")]
        protected MediaHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            IMediaService mediaService,
            ISyncSerializer<IMedia> serializer,
            ISyncTracker<IMedia> tracker,
            AppCaches appCaches,
            ISyncDependencyChecker<IMedia> checker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, appCaches, checker, syncFileService)
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
                var entity = itemFactory.EntityCache.GetEntity(key);
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

        protected override void TerminateEvents(HandlerSettings settings)
        {
            MediaService.Saved -= EventSavedItem;
            MediaService.Deleted -= EventDeletedItem;
            MediaService.Moved -= EventMovedItem;
            MediaService.Trashed -= EventMovedItem;
        }

        protected override IEnumerable<IEntity> GetChildItems(IEntity parent)
        {
            if (parent != null)
            {
                var items = new List<IMedia>();
                const int pageSize = 5000;
                var page = 0;
                var total = long.MaxValue;
                while (page * pageSize < total)
                {
                    items.AddRange(mediaService.GetPagedChildren(parent.Id, page++, pageSize, out total));
                }
                return items;
            }
            else
            {
                return mediaService.GetRootMedia();
            }
        }
    }
}
