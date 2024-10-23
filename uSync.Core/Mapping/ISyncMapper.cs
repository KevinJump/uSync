using Umbraco.Cms.Core.Models;

using uSync.Core.Dependency;

namespace uSync.Core.Mapping;

public interface ISyncMapper
{
    string Name { get; }
    string[] Editors { get; }

    bool IsMapper(PropertyType propertyType);

    //[Obsolete]
    //string? GetExportValue(object value, string editorAlias);

    //[Obsolete]
    //string? GetImportValue(string value, string editorAlias);

    //[Obsolete]
    //IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags);

    Task<string?> GetExportValueAsync(object value, string editorAlias);
    Task<string?> GetImportValueAsync(string value, string editorAlias);

    Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags);


}
