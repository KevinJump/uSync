using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

namespace uSync8.Core.Serialization.Serializers
{
    [USyncSerializer("B3F7F247-6077-406D-8480-DB1004C8211C", "ContentTypeSerializer", uSyncConstants.Serialization.ContentType)]
    public class ContentTypeSerializer : ContentTypeBaseSerializer<IContentType>, ISyncSerializer<IContentType>
    {
        private readonly IContentTypeService contentTypeService;

        public ContentTypeSerializer(
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService) 
            : base(dataTypeService)
        {
            this.contentTypeService = contentTypeService;
        }

        protected override SyncAttempt<XElement> SerializeCore(IContentType item)
        {
            var info = SerializeInfo(item);

            // templates
            info.Add(SerailizeTemplates(item));

            // compositions ? 
            // (might be in the ContentTypeCore, because you can also do this
            //  with media?)
   

            var node = new XElement(ItemType,
                info,
                this.SerializeStructure(item),
                this.SerializeProperties(item),
                this.SerializeTabs(item));

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IContentType), ChangeType.Export);
        }

        private XElement SerailizeTemplates(IContentType item)
        {
            var node = new XElement("AllowedTemplates");
            if (item.AllowedTemplates.Any())
            {
                foreach(var template in item.AllowedTemplates.OrderBy(x => x.Alias))
                {
                    node.Add(new XElement("Template", template.Alias,
                        new XAttribute("Key", template.Key));
                }
            }

            return node;
        }

        protected override SyncAttempt<IContentType> DeserializeCore(XElement node)
        {
            throw new NotImplementedException();
        }

        protected override IContentType LookupById(int id)
        {
            throw new NotImplementedException();
        }
    }
}
