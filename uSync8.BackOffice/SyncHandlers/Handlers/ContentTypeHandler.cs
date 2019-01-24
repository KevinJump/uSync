using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using uSync8.Core;
using uSync8.Core.Serialization;
using uSync8.Core.Serialization.Serializers;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("D1CD424F-2439-4019-93CB-1A300AFB5BEE", "ContentType Handler", "ContentTypes", 1, TwoStep = true)]
    public class ContentTypeHandler : SyncHandlerBase<IContentType>, ISyncHandler
    {
        private readonly IContentTypeService contentTypeService;
        private ISyncSerializer<IContentType> serializer;

        public ContentTypeHandler(
            IEntityService entityService,
            IContentTypeService contentTypeService,
            ISyncSerializer<IContentType> serializer)
            : base(entityService)
        {
            this.contentTypeService = contentTypeService;
            this.serializer = serializer;
        }

        public uSyncAction Export(IContentType item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail(item.Alias, typeof(IContentType), ChangeType.Fail, "Item not set");

            var filename = GetPhysicalPath(folder, item.Alias);

            var attempt = serializer.Serialize(item);
            if (attempt.Success)
            {
                attempt.Item.Save(filename);
            }

            return uSyncActionHelper<XElement>.SetAction(attempt, filename);
        }

        

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            return ExportAll(-1, folder);
        }

        public IEnumerable<uSyncAction> ExportAll(int parent, string folder)
        {
            var actions = new List<uSyncAction>();

            var containers = entityService.GetChildren(parent, UmbracoObjectTypes.DocumentTypeContainer);
            foreach(var container in containers)
            {
                actions.AddRange(ExportAll(container.Id, folder));
            }

            var items = entityService.GetChildren(parent, UmbracoObjectTypes.DocumentType);
            foreach(var item in items)
            {
                var contentType = contentTypeService.Get(item.Id);
                actions.Add(Export(contentType, folder));

                actions.AddRange(ExportAll(item.Id, folder));
            }

            return actions;

        }

        public override SyncAttempt<IContentType> Import(string filePath, bool force = false)
        {
            return new SyncAttempt<IContentType>();
        }

        public override uSyncAction ReportItem(string file)
        {
            return new uSyncAction();
        }

        ////////////////////////////// Event Handlers 
        public void InitializeEvents()
        {
            ContentTypeService.Saved += ContentTypeService_Saved;
            ContentTypeService.Deleted += ContentTypeService_Deleted;
        }

        private void ContentTypeService_Saved(IContentTypeService sender, Umbraco.Core.Events.SaveEventArgs<IContentType> e)
        {
            // do the save thing....
            foreach (var item in e.SavedEntities)
            {
                Export(item, this.DefaultFolder);
            }
        }

        private void ContentTypeService_Deleted(IContentTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IContentType> e)
        {
            // remove the files from the disk, put something in the action log ?
        }

        // /////////////////////// helper functions (will probibly go into base) 

        private string GetPhysicalPath(string folder, string name)
        {
            var folderPath = IOHelper.MapPath($"~/uSync/8/data/{folder}");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var file = Path.Combine(folderPath, name.ToSafeAlias(), ".config");
            if (System.IO.File.Exists(file))
                System.IO.File.Delete(file);

            return file;
        }
    }
}
