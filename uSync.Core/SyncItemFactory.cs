using System.Xml.Linq;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;

using uSync.Core.Cache;
using uSync.Core.Dependency;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.Core;

public class SyncItemFactory : ISyncItemFactory
{
    private readonly SyncTrackerCollection _syncTrackers;
    private readonly SyncDependencyCollection _syncCheckers;

    private readonly SyncEntityCache _entityCache;

    private readonly SyncSerializerCollection _syncSerializers;


    public SyncItemFactory(
        SyncEntityCache entityCache,
        SyncSerializerCollection syncSerializers,
        SyncTrackerCollection syncTrackers,
        SyncDependencyCollection syncCheckers)
    {
        _syncSerializers = syncSerializers;
        _syncTrackers = syncTrackers;
        _syncCheckers = syncCheckers;
        _entityCache = entityCache;
    }

    public SyncEntityCache EntityCache => _entityCache;

    public IEnumerable<ISyncSerializer<TObject>> GetSerializers<TObject>()
        => _syncSerializers.GetSerializers<TObject>();

    public IEnumerable<ISyncEntityContainerSerializer<TObject>> GetContainerSerializers<TObject>(UmbracoObjectTypes containedType)
        where TObject : ITreeEntity
        => _syncSerializers.GetSerializers<TObject>()
            .OfType<ISyncEntityContainerSerializer<TObject>>()
            .Where(x => x.ContainedType == containedType);


    public ISyncSerializer<TObject>? GetSerializer<TObject>(string name)
        => _syncSerializers.GetSerializer<TObject>(name);

    public IEnumerable<ISyncTracker<TObject>> GetTrackers<TObject>()
        => _syncTrackers.GetTrackers<TObject>();

    public async Task<IEnumerable<uSyncChange>> GetChangesAsync<TObject>(XElement node, SyncSerializerOptions options)
        => await _syncTrackers.GetChangesAsync<TObject>(node, options);

    public async Task<IEnumerable<uSyncChange>> GetChangesAsync<TObject>(XElement node, XElement currentNode, SyncSerializerOptions options)
    {
        if (currentNode == null)
            return await _syncTrackers.GetChangesAsync<TObject>(node, options);
        else
            return await _syncTrackers.GetChangesAsync<TObject>(node, currentNode, options);
    }

    public IEnumerable<ISyncDependencyChecker<TObject>> GetCheckers<TObject>()
        => _syncCheckers.GetCheckers<TObject>();

    public async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync<TObject>(TObject item, DependencyFlags flags)
    {
        var dependencies = new List<uSyncDependency>();
        foreach (var checker in _syncCheckers.GetCheckers<TObject>())
        {
            if (checker is null) continue;
            dependencies.AddRange(await checker.GetDependenciesAsync(item, flags) ?? []);
        }
        return dependencies;
    }

}
