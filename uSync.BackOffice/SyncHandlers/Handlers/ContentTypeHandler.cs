using System;

using Microsoft.Extensions.Logging;

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
    ///  Handler to mange content types in uSync
    /// </summary>
    [SyncHandler(uSyncConstants.Handlers.ContentTypeHandler, "DocTypes", "ContentTypes", uSyncConstants.Priorites.ContentTypes,
            IsTwoPass = true, Icon = "icon-item-arrangement", EntityType = UdiEntityType.DocumentType)]
    public class ContentTypeHandler : ContentTypeBaseHandler<IContentType, IContentTypeService>, ISyncHandler, ISyncPostImportHandler, ISyncGraphableHandler,
        INotificationHandler<SavedNotification<IContentType>>,
        INotificationHandler<DeletedNotification<IContentType>>,
        INotificationHandler<MovedNotification<IContentType>>,
        INotificationHandler<EntityContainerSavedNotification>,
        INotificationHandler<EntityContainerRenamedNotification>
    {
        private readonly IContentTypeService contentTypeService;

        /// <summary>
        ///  Constructor - loaded via DI
        /// </summary>
        public ContentTypeHandler(
            ILogger<ContentTypeHandler> logger,
            IEntityService entityService,
            IContentTypeService contentTypeService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
        {
            this.contentTypeService = contentTypeService;
        }

        /// <summary>
        ///  Get the entity name we are going to use when constructing a generic path for an item
        /// </summary>
        protected override string GetEntityTreeName(IUmbracoEntity item, bool useGuid)
        {
            if (useGuid) return item.Key.ToString();

            if (item is IContentTypeBase baseItem)
            {
                return baseItem.Alias.ToSafeFileName(shortStringHelper);
            }

            return item.Name.ToSafeFileName(shortStringHelper);
        }


        /// <summary>
        ///  Fetch a ContentType container via the ContentTypeService
        /// </summary>
        protected override IEntity GetContainer(int id)
            => contentTypeService.GetContainer(id);

        /// <summary>
        ///  Fetch a ContentType container via the ContentTypeService
        /// </summary>
        protected override IEntity GetContainer(Guid key)
            => contentTypeService.GetContainer(key);

        /// <summary>
        ///  Delete a ContentType container via the ContentTypeService
        /// </summary>
        protected override void DeleteFolder(int id)
            => contentTypeService.DeleteContainer(id);            
    }
}
