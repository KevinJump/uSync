using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("mediaTypeHandler", "Media Types", "MediaTypes", uSyncBackOfficeConstants.Priorites.MediaTypes, IsTwoPass = true, Icon = "icon-thumbnails")]
    public class MediaTypeHandler : SyncHandlerTreeBase<IMediaType, IMediaTypeService>, ISyncHandler
    {
        private readonly IMediaTypeService mediaTypeService;

        public MediaTypeHandler(
            IEntityService entityService, 
            IMediaTypeService mediaTypeService,
            IProfilingLogger logger, 
            ISyncSerializer<IMediaType> serializer,
            ISyncTracker<IMediaType> tracker,
            SyncFileService syncFileService, 
            uSyncBackOfficeSettings settings) 
            : base(entityService, logger, serializer, tracker, syncFileService, settings)
        {
            this.mediaTypeService = mediaTypeService;

            this.itemObjectType = UmbracoObjectTypes.MediaType;
            this.itemContainerType = UmbracoObjectTypes.MediaTypeContainer;
        }


        protected override IMediaType GetFromService(int id)
            => mediaTypeService.Get(id);

        protected override string GetItemFileName(IUmbracoEntity item)
            => item.Name;

        public void InitializeEvents()
        {
            MediaTypeService.Saved += ItemSavedEvent;
            MediaTypeService.Deleted += ItemDeletedEvent;
        }

        protected override void DeleteFolder(int id)
            => mediaTypeService.DeleteContainer(id);

        protected override IMediaType GetFromService(Guid key)
            => mediaTypeService.Get(key);

        protected override IMediaType GetFromService(string alias)
            => mediaTypeService.Get(alias);

        protected override void DeleteViaService(IMediaType item)
            => mediaTypeService.Delete(item);
    }
}
