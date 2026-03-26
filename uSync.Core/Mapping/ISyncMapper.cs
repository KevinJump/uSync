using Umbraco.Cms.Core.Models;

using uSync.Core.Dependency;

namespace uSync.Core.Mapping;

public interface ISyncMapper
{
    string Name { get; }
    string[] Editors { get; }

    bool IsMapper(string editorAlias);
    bool IsMapper(PropertyType propertyType);

    Task<string?> GetExportValueAsync(object value, string editorAlias);
    Task<string?> GetImportValueAsync(string value, string editorAlias);

    Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags);
}

public interface ISyncPropertyMapper : ISyncMapper
{
    Task<string?> GetImportValueAsync(string value, IPropertyType propertyType);

}
