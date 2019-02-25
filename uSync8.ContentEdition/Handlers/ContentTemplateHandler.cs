using System;
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
using uSync8.ContentEdition.Serializers;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Handlers
{
    [SyncHandler("contentTemplateHandler", "Blueprints", "Blueprints", uSyncBackOfficeConstants.Priorites.ContentTemplate
        , Icon = "icon-document-dashed-line usync-addon-icon", IsTwoPass = true)]
    public class ContentTemplateHandler : SyncHandlerTreeBase<IContent, IContentService>, ISyncHandler
    {
        private readonly IContentService contentService;

        public ContentTemplateHandler(
            IEntityService entityService, 
            IProfilingLogger logger, 
            IContentService contentService,
            ContentTemplateSerializer serializer, // concreate because we want to make sure we get the blueprint one.
            ISyncTracker<IContent> tracker, 
            SyncFileService syncFileService) 
            : base(entityService, logger, serializer, tracker, syncFileService)
        {
            this.contentService = contentService;
            this.itemObjectType = UmbracoObjectTypes.DocumentBlueprint;
        }

        protected override void DeleteViaService(IContent item)
            => contentService.DeleteBlueprint(item);

        protected override IContent GetFromService(int id)
            => contentService.GetBlueprintById(id);

        protected override IContent GetFromService(Guid key)
            => contentService.GetBlueprintById(key);

        protected override IContent GetFromService(string alias)
            => null;

        protected override void InitializeEvents(HandlerSettings settings)
        {
            ContentService.SavedBlueprint += EventSavedItem;
            ContentService.DeletedBlueprint += EventDeletedItem;
        }
    }
}
