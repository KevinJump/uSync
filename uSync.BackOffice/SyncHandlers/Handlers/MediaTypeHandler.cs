using System;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

using static Umbraco.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("mediaTypeHandler", "Media Types", "MediaTypes", uSyncBackOfficeConstants.Priorites.MediaTypes,
        IsTwoPass = true, Icon = "icon-thumbnails", EntityType = UdiEntityType.MediaType)]
    public class MediaTypeHandler : SyncHandlerContainerBase<IMediaType, IMediaTypeService>, ISyncExtendedHandler
    {
        private readonly IMediaTypeService mediaTypeService;

        public MediaTypeHandler(
            IShortStringHelper shortStringHelper,
            IMediaTypeService mediaTypeService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IMediaType> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(shortStringHelper, entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)

        {
            this.mediaTypeService = mediaTypeService;
        }




        protected override void InitializeEvents(HandlerSettings settings)
        {
            MediaTypeService.Saved += EventSavedItem;
            MediaTypeService.Deleted += EventDeletedItem;
            MediaTypeService.Moved += EventMovedItem;

            MediaTypeService.SavingContainer += EventContainerSaved;
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
          


        protected override IMediaType GetFromService(int id)
            => mediaTypeService.Get(id);

        protected override IMediaType GetFromService(Guid key)
            => mediaTypeService.Get(key);

        protected override IMediaType GetFromService(string alias)
            => mediaTypeService.Get(alias);

        protected override void DeleteViaService(IMediaType item)
            => mediaTypeService.Delete(item);

        protected override void DeleteFolder(int id)
            => mediaTypeService.DeleteContainer(id);

        protected override string GetItemAlias(IMediaType item)
            => item.Alias;

        protected override IEntity GetContainer(int id)
            => mediaTypeService.GetContainer(id);

        protected override IEntity GetContainer(Guid key)
            => mediaTypeService.GetContainer(key);

    }
}
