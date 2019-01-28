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
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;
using uSync8.Core.Tracking.Impliment;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("templateHandler", "Templates", "Templates", uSyncBackOfficeConstants.Priorites.Templates, Icon = "icon-layout")]
    public class TemplateHandler : SyncHandlerBase<ITemplate, IFileService>, ISyncHandler
    {
        private readonly IFileService fileService;

        public TemplateHandler(
            IEntityService entityService, 
            IProfilingLogger logger, 
            IFileService fileService,
            ISyncSerializer<ITemplate> serializer, 
            ISyncTracker<ITemplate> tracker,
            SyncFileService syncFileService, 
            uSyncBackOfficeSettings settings) 
            : base(entityService, logger, serializer, tracker, syncFileService, settings)
        {
            this.fileService = fileService;

            this.itemObjectType = UmbracoObjectTypes.Template;

            // this might need some work - because its not a container thing ?
            this.itemContainerType = UmbracoObjectTypes.Unknown;
        }

        protected override ITemplate GetFromService(int id)
            => fileService.GetTemplate(id);

        public void InitializeEvents()
        {
            FileService.SavedTemplate += ItemSavedEvent;
            FileService.DeletedTemplate += ItemDeletedEvent;
        }

        protected override string GetItemPath(ITemplate item)
            => item.Name;

        protected override ITemplate GetFromService(Guid key)
            => fileService.GetTemplate(key);

        protected override ITemplate GetFromService(string alias)
            => fileService.GetTemplate(alias);

        protected override void DeleteViaService(ITemplate item)
            => fileService.DeleteTemplate(item.Alias);

        protected override string GetItemName(ITemplate item)
            => item.Name;
    }
}
