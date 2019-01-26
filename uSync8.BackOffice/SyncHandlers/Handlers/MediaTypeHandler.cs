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

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("mediaTypeHandler", "Media Type Handler", "MediaTypes", uSyncBackOfficeConstants.Priorites.MediaTypes, IsTwoPass = true, Icon = "icon-thumbnails")]
    public class MediaTypeHandler : SyncHandlerTreeBase<IMediaType, IMediaTypeService>, ISyncHandler
    {
        private readonly IMediaTypeService mediaTypeService;

        public MediaTypeHandler(
            IEntityService entityService, 
            IMediaTypeService mediaTypeService,
            IProfilingLogger logger, 
            ISyncSerializer<IMediaType> serializer,
            SyncFileService syncFileService, 
            uSyncBackOfficeSettings settings) 
            : base(entityService, logger, 
                  serializer, syncFileService, settings)
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

        public override uSyncAction ReportItem(string file)
        {
            return uSyncAction.Fail("not implimented", typeof(IMediaType), new Exception("Not implimented"));
        }

    }
}
