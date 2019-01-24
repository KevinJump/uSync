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
    [SyncHandler("mediaTypeHandler", "Media Type Handler", "MediaTypes", 2, IsTwoPass = true)]
    public class MediaTypeHandler : SyncHandlerBase<IMediaType>, ISyncHandler
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

        #region Import

        #endregion

        #region Export 

        #endregion

        protected override IMediaType GetFromService(int id)
            => mediaTypeService.Get(id);

        protected override string GetItemFileName(IUmbracoEntity item)
            => item.Name;

        public void InitializeEvents()
        {
            MediaTypeService.Saved += MediaTypeService_Saved;
            MediaTypeService.Deleted += MediaTypeService_Deleted;
        }

        private void MediaTypeService_Deleted(IMediaTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IMediaType> e)
        {
            // throw new NotImplementedException();
        }

        private void MediaTypeService_Saved(IMediaTypeService sender, Umbraco.Core.Events.SaveEventArgs<IMediaType> e)
        {
            foreach(var item in e.SavedEntities)
            {
                Export(item, this.DefaultFolder);
            }
        }

        public override uSyncAction ReportItem(string file)
        {
            return uSyncAction.Fail("not implimented", typeof(IMediaType), new Exception("Not implimented"));
        }

    }
}
