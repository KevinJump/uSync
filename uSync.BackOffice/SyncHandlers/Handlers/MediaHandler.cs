using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Serialization;
using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("mediaHandler", "Media", "Media", uSyncBackOfficeConstants.Priorites.Media,
        Icon = "icon-picture usync-addon-icon", IsTwoPass = true, EntityType = UdiEntityType.Media)]
    public class MediaHandler : ContentHandlerBase<IMedia, IMediaService>, ISyncHandler, ISyncExtendedHandler, ISyncItemHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IMediaService mediaService;

        public MediaHandler(
            IShortStringHelper shortStringHelper,
            ILogger<MediaHandler> logger,
            uSyncConfigService uSyncConfigService,
            AppCaches appCaches,
            ISyncSerializer<IMedia> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService,            
            IEntityService entityService,
            IMediaService mediaService)
            : base(shortStringHelper, logger, uSyncConfigService, appCaches, serializer, syncItemFactory, syncFileService, entityService)
        {
            this.mediaService = mediaService;
        }

        protected override void DeleteViaService(IMedia item)
            => mediaService.Delete(item);

        protected override IMedia GetFromService(int id)
            => mediaService.GetById(id);

        protected override IMedia GetFromService(Guid key)
        {    
            return mediaService.GetById(key);
        }


        protected override IMedia GetFromService(string alias)
            => null;


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
