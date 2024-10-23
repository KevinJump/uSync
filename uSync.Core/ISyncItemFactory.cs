using System.Xml.Linq;

using uSync.Core.Cache;
using uSync.Core.Dependency;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.Core;

/// <summary>
///  Factory for getting changes or dependencies from an item.
/// </summary>
public interface ISyncItemFactory
{
    // serializers
    IEnumerable<ISyncSerializer<TObject>> GetSerializers<TObject>();

    ISyncSerializer<TObject>? GetSerializer<TObject>(string name);

    // tracking items
    IEnumerable<ISyncTracker<TObject>> GetTrackers<TObject>();

    [Obsolete("use GetChangesAsync will be removed in v16")]
    IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, SyncSerializerOptions options)
        => GetChangesAsync<TObject>(node, options).Result;
    Task<IEnumerable<uSyncChange>> GetChangesAsync<TObject>(XElement node, SyncSerializerOptions options);

    [Obsolete("use GetChangesAsync will be removed in v16")]
    IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, XElement currentNode, SyncSerializerOptions options)
        => GetChangesAsync<TObject>(node, currentNode, options).Result;

    Task<IEnumerable<uSyncChange>> GetChangesAsync<TObject>(XElement node, XElement currentNode, SyncSerializerOptions options);

    // dependency checker items
    IEnumerable<ISyncDependencyChecker<TObject>> GetCheckers<TObject>();

    [Obsolete("Use GetDependenciesAsync will be removed in v16")]
    IEnumerable<uSyncDependency> GetDependencies<TObject>(TObject item, DependencyFlags flags)
        => GetDependenciesAsync(item, flags).Result;

    Task<IEnumerable<uSyncDependency>> GetDependenciesAsync<TObject>(TObject item, DependencyFlags flags);
    
    SyncEntityCache EntityCache { get; }

}