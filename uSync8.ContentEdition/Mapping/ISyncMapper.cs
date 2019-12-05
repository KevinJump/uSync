using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping
{
    public interface ISyncMapper
    {
        string Name { get; }
        string[] Editors { get; }

        bool IsMapper(PropertyType propertyType);

        string GetExportValue(object value, string editorAlias);
        string GetImportValue(string value, string editorAlias);

        IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags);
    }
}
