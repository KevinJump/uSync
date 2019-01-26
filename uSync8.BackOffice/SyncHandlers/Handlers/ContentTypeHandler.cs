using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Serialization;
using uSync8.Core.Serialization.Serializers;
using uSync8.Core.Tracking;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("contentTypeHandler", "ContentType Handler", "ContentTypes", uSyncBackOfficeConstants.Priorites.ContentTypes, IsTwoPass = true, Icon = "icon-item-arrangement")]
    public class ContentTypeHandler : SyncHandlerTreeBase<IContentType, IContentTypeService>, ISyncHandler
    {
        private readonly IContentTypeService contentTypeService;

        public ContentTypeHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            IContentTypeService contentTypeService,
            ISyncSerializer<IContentType> serializer,
            ISyncTracker<IContentType> tracker,
            SyncFileService fileService,
            uSyncBackOfficeSettings settings)
            : base(entityService, logger, serializer, tracker, fileService, settings)
        {
            this.contentTypeService = contentTypeService;

            this.itemObjectType = UmbracoObjectTypes.DocumentType;
            this.itemContainerType = UmbracoObjectTypes.DocumentTypeContainer;

        }


        #region Import
            // default import behavior is in the base class.
        #endregion

        #region Export
            // most of export is now in the base 


        #endregion
      
        public void InitializeEvents()
        {
            ContentTypeService.Saved += ItemSavedEvent;
            ContentTypeService.Deleted += ItemDeletedEvent;
        }

        protected override IContentType GetFromService(int id)
            => contentTypeService.Get(id);

        protected override string GetItemFileName(IUmbracoEntity item)
            => item.Name;


    }
}
