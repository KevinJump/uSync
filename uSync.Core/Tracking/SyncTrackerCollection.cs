using System.Xml.Linq;

using Umbraco.Cms.Core.Composing;
using Umbraco.Extensions;

using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking;

public class SyncTrackerCollection : BuilderCollectionBase<ISyncTrackerBase>
{
    public SyncTrackerCollection(Func<IEnumerable<ISyncTrackerBase>> items)
        : base(items)
    { }

    public IEnumerable<ISyncTracker<TObject>> GetTrackers<TObject>()
    {
        return this.Where(x => x is ISyncTracker<TObject> tracker)
            .Select(x => x as ISyncTracker<TObject>).WhereNotNull();
    }

    [Obsolete("use GetChangesAsync will be removed in v16")]
    public IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, SyncSerializerOptions options)
        => GetChangesAsync<TObject>(node, options).Result;
    
    public async Task<IEnumerable<uSyncChange>> GetChangesAsync<TObject>(XElement node, SyncSerializerOptions options)
    {
        var changes = new List<uSyncChange>();
        foreach (var tracker in GetTrackers<TObject>())
        {
            if (tracker is null) continue;
            changes.AddRange(await tracker.GetChangesAsync(node, options));
        }
        return changes;
    }

    [Obsolete("use GetChangesAsync will be removed in v16")]
    public IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, XElement currentNode, SyncSerializerOptions options)
        => GetChangesAsync<TObject>(node, currentNode, options).Result;
    
    public async Task<IEnumerable<uSyncChange>> GetChangesAsync<TObject>(XElement node, XElement currentNode, SyncSerializerOptions options)
    {
        if (currentNode == null)
            return await GetChangesAsync<TObject>(node, options);

        var changes = new List<uSyncChange>();
        foreach (var tracker in GetTrackers<TObject>())
        {
            if (tracker is null) continue;
            changes.AddRange(await tracker.GetChangesAsync(node, currentNode, options));
        }
        return changes;
    }

}



public class SyncTrackerCollectionBuilder
    : WeightedCollectionBuilderBase<SyncTrackerCollectionBuilder,
        SyncTrackerCollection, ISyncTrackerBase>
{
    protected override SyncTrackerCollectionBuilder This => this;
}
