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

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("templateHandler", "Template Handler", "Templates", uSyncBackOfficeConstants.Priorites.Templates)]
    public class TemplateHandler : SyncHandlerBase<ITemplate>, ISyncHandler
    {
        private readonly IFileService fileService;

        public TemplateHandler(
            IEntityService entityService, 
            IProfilingLogger logger, 
            IFileService fileService,
            ISyncSerializer<ITemplate> serializer, 
            SyncFileService syncFileService, 
            uSyncBackOfficeSettings settings) 
            : base(entityService, logger, serializer, syncFileService, settings)
        {
            this.fileService = fileService;

            this.itemObjectType = UmbracoObjectTypes.Template;

            // this might need some work - because its not a container thing ?
            this.itemContainerType = UmbracoObjectTypes.Unknown;
        }

        public override uSyncAction ReportItem(string file)
        {
            return uSyncAction.Fail("not implimented", typeof(IMemberType), new Exception("Not implimented"));
        }

        protected override ITemplate GetFromService(int id)
            => fileService.GetTemplate(id);

        public void InitializeEvents()
        {
            FileService.SavedTemplate += FileService_SavedTemplate;
            FileService.DeletedTemplate += FileService_DeletedTemplate;
        }

        private void FileService_DeletedTemplate(IFileService sender, Umbraco.Core.Events.DeleteEventArgs<ITemplate> e)
        {
            // not yet
        }

        private void FileService_SavedTemplate(IFileService sender, Umbraco.Core.Events.SaveEventArgs<ITemplate> e)
        {
            foreach(var item in e.SavedEntities)
            {
                Export(item, this.DefaultFolder);
            }
        }

        protected override string GetItemPath(ITemplate item)
            => item.Name;
    }
}
