using Umbraco.Cms.Core.Models;

using uSync.Core.Dependency;
using uSync.Core.Serialization;

namespace uSync.Core.Mapping;

public interface ISyncMapper
{
    string Name { get; }
    string[] Editors { get; }

    bool IsMapper(string editorAlias) 
        => Editors.Contains(editorAlias, StringComparer.OrdinalIgnoreCase);

    bool IsMapper(PropertyType propertyType);

    Task<string?> GetExportValueAsync(object value, string editorAlias);

    [Obsolete("Use GetImportValueAsync(string value, string editorAlias, SyncSerializerOptions options) instead will be removed in v19")]
    Task<string?> GetImportValueAsync(string value, string editorAlias);

    Task<string?> GetImportValueAsync(string value, string editorAlias, SyncSerializerOptions options)
#pragma warning disable CS0618 // Type or member is obsolete
        => GetImportValueAsync(value, editorAlias);
#pragma warning restore CS0618 // Type or member is obsolete

    Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags);
}

public interface ISyncPropertyMapper : ISyncMapper
{
    Task<string?> GetImportValueAsync(string value, IPropertyType propertyType, SyncSerializerOptions options);

}
