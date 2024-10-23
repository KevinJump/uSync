using Umbraco.Cms.Core.Models;

namespace uSync.Core.Dependency;

/// <summary>
///   Dependency Item
/// </summary>
public interface ISyncDependencyItem
{
    UmbracoObjectTypes ObjectType { get; }
}

/// <summary>
///  Check to generate a list of dependencies.
/// </summary>
/// <typeparam name="TObject"></typeparam>
public interface ISyncDependencyChecker<TObject> : ISyncDependencyItem
{
    /// <summary>
    ///  calculate the dependencies for an item based on the passed flags.
    /// </summary>
    [Obsolete("Use GetDependenciesAsync will be removed in v16")]
    IEnumerable<uSyncDependency> GetDependencies(TObject item, DependencyFlags flags)
        => GetDependenciesAsync(item, flags).Result;

    Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(TObject item, DependencyFlags flags);
}
