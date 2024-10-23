using uSync.Core.Dependency;

namespace uSync.Core.Mapping;

public static class SyncValueMapperCollectionExtensions
{
    [Obsolete("use GetDependenciesAsync will be removed in v16")]
    public static IEnumerable<uSyncDependency> GetDependencies(
        this SyncValueMapperCollection mapperCollection,
        object value, string editorAlias, DependencyFlags flags)
     => mapperCollection.GetDependenciesAsync(value, editorAlias, flags).Result;

    public static async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(
        this SyncValueMapperCollection mapperCollection,
        object value, string editorAlias, DependencyFlags flags)
    { 
        var mappers = mapperCollection.GetSyncMappers(editorAlias);
        if (!mappers.Any()) return [];

        var dependencies = new List<uSyncDependency>();
        foreach (var mapper in mappers)
        {
            dependencies.AddRange(await mapper.GetDependenciesAsync(value, editorAlias, flags));
        }
        return dependencies;
    }
}
