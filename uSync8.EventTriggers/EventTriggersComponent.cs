using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.BackOffice;

namespace ExampleuSyncEvents
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class EventTriggerComposer : ComponentComposer<EventTriggersComponent> { }

    /// <summary>
    ///  Component, will trigger a cache rebuild when an import is completed. (and there are changes)
    /// </summary>
    public class EventTriggersComponent : IComponent
    {
        private readonly IEntityService entityService;

        private static string blockedContainer = "Compositions";
        private int componentsFolderId = -1;


        public EventTriggersComponent(IEntityService entityService)
        {
            this.entityService = entityService;
        }

        public void Initialize()
        {
            var rootContainers = entityService.GetChildren(-1, UmbracoObjectTypes.DocumentTypeContainer);
            componentsFolderId = rootContainers.FirstOrDefault(x => x.Name.InvariantEquals(blockedContainer))?.Id ?? -1;

            uSyncService.ImportingItem += USyncService_ImportingItem;
            uSyncService.ExportingItem += USyncService_ExportingItem;
        }

        /// <summary>
        ///  stop an item from being exported if its in the folder
        /// </summary>
        private void USyncService_ExportingItem(uSyncItemEventArgs<object> e)
        {
            if (componentsFolderId != -1 && e.Item is IContentType contentType)
            {
                if (contentType.ParentId == componentsFolderId)
                    e.Cancel = true;
            }
        }

        /// <summary>
        ///  stop an item being imported if its in a folder. 
        /// </summary>
        private void USyncService_ImportingItem(uSyncItemEventArgs<XElement> e)
        {
            if (e.Item.Name.LocalName == "ContentType")
            {
                var folder = e.Item.Element("Info")?.Element("Folder").Value;
                if (!string.IsNullOrWhiteSpace(folder) && folder.InvariantEquals(blockedContainer))
                {
                    e.Cancel = true;
                }
            }
        }

        public void Terminate()
        {
            // end
        }
    }
}
