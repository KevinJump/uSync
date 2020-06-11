﻿using System;

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
    [SyncHandler("mediaTypeHandler", "Media Types", "MediaTypes", uSyncBackOfficeConstants.Priorites.MediaTypes,
        IsTwoPass = true, Icon = "icon-thumbnails", EntityType = UdiEntityType.MediaType)]
    public class MediaTypeHandler : SyncHandlerContainerBase<IMediaType, IMediaTypeService>, ISyncExtendedHandler
    {
        private readonly IMediaTypeService mediaTypeService;

        public MediaTypeHandler(
            IEntityService entityService, 
            IMediaTypeService mediaTypeService,
            IProfilingLogger logger, 
            ISyncSerializer<IMediaType> serializer,
            SyncTrackerCollection trackers,
            AppCaches appCaches,
            SyncDependencyCollection checkers,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, trackers, appCaches, checkers, syncFileService)
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
                return mediaType.Alias.ToSafeFileName();
            }

            return item.Name.ToSafeFileName();
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
    }
}
