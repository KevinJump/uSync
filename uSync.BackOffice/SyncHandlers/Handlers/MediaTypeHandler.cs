using Microsoft.Extensions.Logging;

using System;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    /// <summary>
    ///  Handler to mange Media Types in uSync
    /// </summary>
    [SyncHandler(uSyncConstants.Handlers.MediaTypeHandler, "Media Types", "MediaTypes", uSyncConstants.Priorites.MediaTypes,
        IsTwoPass = true, Icon = "icon-thumbnails", EntityType = UdiEntityType.MediaType)]
    public class MediaTypeHandler : SyncHandlerContainerBase<IMediaType, IMediaTypeService>, ISyncHandler,
        INotificationHandler<SavedNotification<IMediaType>>,
        INotificationHandler<DeletedNotification<IMediaType>>,
        INotificationHandler<MovedNotification<IMediaType>>,
        INotificationHandler<EntityContainerSavedNotification>,        
        INotificationHandler<EntityContainerRenamedNotification>
    // INotificationHandler<MediaTypeSavedNotification>
    {
        private readonly IMediaTypeService mediaTypeService;

        /// <inheritdoc/>
        public MediaTypeHandler(
            ILogger<MediaTypeHandler> logger,
            IEntityService entityService,
            IMediaTypeService mediaTypeService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)

        {
            this.mediaTypeService = mediaTypeService;
        }

        /// <inheritdoc/>
        protected override string GetEntityTreeName(IUmbracoEntity item, bool useGuid)
        {
            if (useGuid) return item.Key.ToString();

            if (item is IMediaType mediaType)
            {
                return mediaType.Alias.ToSafeFileName(shortStringHelper);
            }

            return item.Name.ToSafeFileName(shortStringHelper);
        }

        /// <inheritdoc/>
        protected override void DeleteFolder(int id)
            => mediaTypeService.DeleteContainer(id);

        /// <inheritdoc/>
        protected override IEntity GetContainer(int id)
            => mediaTypeService.GetContainer(id);

        /// <inheritdoc/>
        protected override IEntity GetContainer(Guid key)
            => mediaTypeService.GetContainer(key);

    }
}
