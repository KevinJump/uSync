﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Handlers
{
    [SyncHandler("mediaHandler", "Media", "Media", uSyncBackOfficeConstants.Priorites.Media,
        Icon = "icon-picture usync-addon-icon", IsTwoPass = true)]
    public class MediaHandler : SyncHandlerTreeBase<IMedia, IMediaService>, ISyncHandler
    {
        private readonly IMediaService mediaService;

        public MediaHandler(
            IEntityService entityService, 
            IProfilingLogger logger, 
            IMediaService mediaService,
            ISyncSerializer<IMedia> serializer, 
            ISyncTracker<IMedia> tracker, 
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, syncFileService)
        {
            this.mediaService = mediaService;
            this.itemObjectType = UmbracoObjectTypes.Media;
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
        }
    }
}
