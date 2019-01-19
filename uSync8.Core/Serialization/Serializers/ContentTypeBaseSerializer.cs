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
    public abstract class ContentTypeBaseSerializer<TObject> : USyncSerializerBase<TObject>
        where TObject : IContentTypeBase
    {
        private readonly IDataTypeService dataTypeService;

        public ContentTypeBaseSerializer(IDataTypeService dataTypeService)
        {
            this.dataTypeService = dataTypeService;
        }
    

        protected XElement SerializeInfo(TObject item)
        {
            return new XElement("Info",
                            new XElement("Key", item.Key),
                            new XElement("Name", item.Name),
                            new XElement("Alias", item.Alias),
                            new XElement("Icon", item.Icon),
                            new XElement("Thumbnail", item.Thumbnail),
                            new XElement("Description", string.IsNullOrWhiteSpace(item.Description) ? "" : item.Description),
                            new XElement("AllowAtRoot", item.AllowedAsRoot.ToString()),
                            new XElement("IsListView", item.IsContainer.ToString()));
        }

        protected XElement SerializeTabs(TObject item)
        {
            var tabs = new XElement("Tabs");

            foreach(var tab in item.PropertyGroups.OrderBy(x => x.SortOrder))
            {
                tabs.Add(new XElement("Tab",
                            new XElement("Key", tab.Key),
                            new XElement("Caption", tab.Name),
                            new XElement("SortOrder", tab.SortOrder)));
            }

            return tabs;
        }

        protected XElement SerializeProperties(TObject item)
        {
            var node = new XElement("GenericProperties");

            foreach(var property in item.PropertyTypes.OrderBy(x => x.Alias))
            {
                var propNode = new XElement("GenericProperty",
                    new XElement("Key", property.Key),
                    new XElement("Name", property.Name),
                    new XElement("Alias", property.Alias));

                var def = dataTypeService.GetDataType(property.DataTypeId);
                if (def != null)
                {
                    propNode.Add(new XElement("Definition", def.Key));
                    propNode.Add(new XElement("Type", def.EditorAlias));
                }
                else
                {
                    propNode.Add(new XElement("Type", property.PropertyEditorAlias));
                }

                propNode.Add(new XElement("Mandatory", property.Mandatory));
                propNode.Add(new XElement("Validation",
                    string.IsNullOrEmpty(property.ValidationRegExp) ? "" : property.ValidationRegExp));

                var description = string.IsNullOrEmpty(property.Description) ? "" : property.Description;
                propNode.Add(new XElement("Description", new XCData(description)));

                propNode.Add(new XElement("SortOrder", property.SortOrder));

                var tab = item.PropertyGroups.FirstOrDefault(x => x.PropertyTypes.Contains(property));
                propNode.Add(new XElement("Tab", tab != null ? tab.Name : ""));

                node.Add(propNode);
            }

            return node;
        }

        protected XElement SerializeStructure(TObject item)
        {
            var node = new XElement("Structure");

            foreach(var allowedType in item.AllowedContentTypes.OrderBy(x => x.Alias))
            {
                var allowedItem = LookupById(allowedType.Id.Value);
                if (allowedItem != null)
                {
                    node.Add(new XElement(ItemType, allowedItem.Key.ToString()));
                }
            }

            return node;
        }


        protected abstract TObject LookupById(int id);
    }
}
