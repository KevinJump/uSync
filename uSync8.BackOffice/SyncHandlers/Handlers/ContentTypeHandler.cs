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
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Serialization.Serializers;
using uSync8.Core.Tracking;
using static Umbraco.Core.Constants;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("contentTypeHandler", "DocTypes", "ContentTypes", uSyncBackOfficeConstants.Priorites.ContentTypes, 
            IsTwoPass = true, Icon = "icon-item-arrangement", EntityType = UdiEntityType.DocumentType)]
    public class ContentTypeHandler : SyncHandlerContainerBase<IContentType, IContentTypeService>, ISyncSingleItemHandler, ISyncPostImportHandler
    {
        private readonly IContentTypeService contentTypeService;

        public ContentTypeHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            IContentTypeService contentTypeService,
            ISyncSerializer<IContentType> serializer,
            ISyncTracker<IContentType> tracker,
            ISyncDependencyChecker<IContentType> checker,
            SyncFileService fileService)
            : base(entityService, logger, serializer, tracker, checker, fileService)
        {
            this.contentTypeService = contentTypeService;
        }


        #region Import
            // default import behavior is in the base class.
        #endregion

        #region Export
            // most of export is now in the base 


        #endregion
      
            
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

        protected override void DeleteFolder(int id)
            => contentTypeService.DeleteContainer(id);

        protected override void DeleteViaService(IContentType item)
            => contentTypeService.Delete(item);

    }
}
