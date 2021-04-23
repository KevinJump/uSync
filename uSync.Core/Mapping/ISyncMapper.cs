using System.Collections.Generic;

using Umbraco.Cms.Core.Models;

using uSync.Core.Dependency;

namespace uSync.Core.Mapping
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
