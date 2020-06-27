using System;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("contentTypeHandler", "DocTypes", "ContentTypes", uSyncBackOfficeConstants.Priorites.ContentTypes,
            IsTwoPass = true, Icon = "icon-item-arrangement", EntityType = UdiEntityType.DocumentType)]
    public class ContentTypeHandler : SyncHandlerContainerBase<IContentType>, ISyncExtendedHandler, ISyncPostImportHandler
    {
        private readonly IContentTypeService contentTypeService;

        public ContentTypeHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IContentType> serializer,
            SyncTrackerCollection trackers,
            SyncDependencyCollection checkers,
            SyncFileService fileService,
            IContentTypeService contentTypeService)
            : base(entityService, logger, appCaches, serializer, trackers, checkers, fileService)
        {
            this.contentTypeService = contentTypeService;
        }

        protected override void InitializeEvents(HandlerSettings settings)
        {
            ContentTypeService.Saved += EventSavedItem;
            ContentTypeService.Deleted += EventDeletedItem;
            ContentTypeService.Moved += EventMovedItem;

            ContentTypeService.SavedContainer += EventContainerSaved;
        }

        protected override string GetItemFileName(IUmbracoEntity item, bool useGuid)
        {
            if (useGuid) return item.Key.ToString();

            if (item is IContentType contentItem)
            {
                return contentItem.Alias.ToSafeFileName();
            }

            return item.Name.ToSafeFileName();
        }

        protected override IContentType GetFromService(int id)
            => contentTypeService.Get(id);

        protected override IContentType GetFromService(Guid key)
            => contentTypeService.Get(key);

        protected override IContentType GetFromService(string alias)
            => contentTypeService.Get(alias);

        protected override IEntity GetContainer(Guid key)
            => contentTypeService.GetContainer(key);

        protected override void DeleteFolder(int id)
            => contentTypeService.DeleteContainer(id);

        protected override void DeleteViaService(IContentType item)
            => contentTypeService.Delete(item);

        protected override string GetItemAlias(IContentType item)
            => item.Alias;
    }
}
