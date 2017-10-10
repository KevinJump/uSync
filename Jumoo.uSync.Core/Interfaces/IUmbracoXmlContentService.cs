using System.Xml.Linq;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Interfaces
{
    public interface IUmbracoXmlContentService
    {
        string GetExportValue(Property property);
        string GetImportXml(XElement node);
        string GetInnerXml(XElement node);
        string GetImportXml(string value);
        string ReplaceInnerXml(XElement node, string value);
        string GetImportValue(PropertyType propType, string content);
    }
}