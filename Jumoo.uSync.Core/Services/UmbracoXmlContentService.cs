using System.Linq;
using System.Web;
using System.Xml.Linq;
using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Mappers;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Services
{
    public class UmbracoXmlContentService : IUmbracoXmlContentService
    {
        public string GetExportValue(Property property)
        {
            XElement propertyElement;
            try
            {
                propertyElement = property.ToXml();
            }
            catch
            {
                propertyElement = new XElement(property.Alias, property.Value);
            }

            var importXml = GetImportXml(propertyElement);
            if (string.IsNullOrWhiteSpace(importXml)) return GetInnerXml(propertyElement);
            var mapping = uSyncCoreContext.Instance.Configuration.Settings.ContentMappings.SingleOrDefault(x => x.EditorAlias ==
                property.PropertyType.PropertyEditorAlias);
            if (mapping == null) return GetInnerXml(propertyElement);

            var mapper = ContentMapperFactory.GetMapper(mapping);
            return mapper != null
                ? ReplaceInnerXml(propertyElement, mapper.GetExportValue(property.PropertyType.DataTypeDefinitionId, importXml))
                : GetInnerXml(propertyElement);
        }

        public string GetImportXml(XElement node)
        {
            var str = GetInnerXml(node);
            return str.StartsWith("<![CDATA[")
                ? node.Value
                : str.Replace("&amp;", "&");
        }

        public string GetImportXml(string value)
        {
            return GetImportXml(new XElement("temp", value));
        }

        public string GetInnerXml(XElement parent)
        {
            var reader = parent.CreateReader();
            reader.MoveToContent();
            return HttpUtility.HtmlDecode(reader.ReadInnerXml());
        }

        public string ReplaceInnerXml(XElement parent, string value)
        {
            var reader = parent.CreateReader();
            return reader.ReadInnerXml().StartsWith("<![CDATA[")
                ? $"<![CDATA[{value}]]>"
                : value;
        }


        public string GetImportValue(PropertyType propType, string content)
        {
            var mapper = ContentMapperFactory.GetMapper(propType.PropertyEditorAlias);
            if (mapper != null)
            {
                return mapper.GetImportValue(propType.DataTypeDefinitionId, content);
            }

            var xElement = XElement.Parse($"<temp>{content}</temp>");
            return xElement.Value;
        }
    }
}