using System;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Core.Strings;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync.ContentEdition.Handlers
{
    [SyncHandler("mediaHandler", "Media", "Media", uSyncBackOfficeConstants.Priorites.Media,
        Icon = "icon-picture usync-addon-icon", IsTwoPass = true, EntityType = UdiEntityType.Media)]
    public class MediaHandler : ContentHandlerBase<IMedia, IMediaService>, ISyncHandler, ISyncExtendedHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IMediaService mediaService;

        public MediaHandler(
            IShortStringHelper shortStringHelper,
            IMediaService mediaService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IMedia> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(shortStringHelper, entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
        {
            this.mediaService = mediaService;
        }

        protected override void DeleteViaService(IMedia item)
            => mediaService.Delete(item);

        protected override IMedia GetFromService(int id)
            => mediaService.GetById(id);

        protected override IMedia GetFromService(Guid key)
            => mediaService.GetById(key);

        protected override IMedia GetFromService(string alias)
            => null;

        protected override void InitializeEvents(HandlerSettings settings)
        {
            MediaService.Saved += EventSavedItem;
            MediaService.Deleted += EventDeletedItem;
            MediaService.Moved += EventMovedItem;
            MediaService.Trashed += EventMovedItem;
        }
    }
}
