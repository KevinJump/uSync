using Umbraco.Core.Models;

namespace uSync8.ContentEdition.Mappers
{
    public interface ISyncMapper
    {
        string Name { get; }
        string[] Editors {get;}

        bool IsMapper(PropertyType propertyType);

        string GetExportValue(object value, string editorAlias);
        string GetImportValue(string value, string editorAlias);
            
    }
}
