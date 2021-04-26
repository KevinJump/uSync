using System;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Serialization;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("mediaTypeHandler", "Media Types", "MediaTypes", uSyncBackOfficeConstants.Priorites.MediaTypes,
        IsTwoPass = true, Icon = "icon-thumbnails", EntityType = UdiEntityType.MediaType)]
    public class MediaTypeHandler : SyncHandlerContainerBase<IMediaType, IMediaTypeService>, ISyncHandler,
        INotificationHandler<SavedNotification<IMediaType>>,
        INotificationHandler<DeletedNotification<IMediaType>>,
        INotificationHandler<MovedNotification<IMediaType>>,
        INotificationHandler<EntityContainerSavedNotification>
        // INotificationHandler<MediaTypeSavedNotification>
    {
        private readonly IMediaTypeService mediaTypeService;

        public MediaTypeHandler(
            ILogger<MediaTypeHandler> logger,
            IEntityService entityService,
            IMediaTypeService mediaTypeService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncMutexService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncSerializer<IMediaType> serializer,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, serializer, syncItemFactory)

        {
            this.mediaTypeService = mediaTypeService;
        }       

        protected override string GetItemFileName(IUmbracoEntity item, bool useGuid)
        {
            if (useGuid) return item.Key.ToString();

            if (item is IMediaType mediaType)
            {
                return mediaType.Alias.ToSafeFileName(shortStringHelper);
            }

            return item.Name.ToSafeFileName(shortStringHelper);
        }

        protected override void DeleteFolder(int id)
            => mediaTypeService.DeleteContainer(id);

        protected override IEntity GetContainer(int id)
            => mediaTypeService.GetContainer(id);

        protected override IEntity GetContainer(Guid key)
            => mediaTypeService.GetContainer(key);

    }
}
