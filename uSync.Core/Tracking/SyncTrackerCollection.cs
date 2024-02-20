using System.Xml.Linq;

using Umbraco.Cms.Core.Composing;

using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking;

public class SyncTrackerCollection
       : BuilderCollectionBase<ISyncTrackerBase>
{
    public SyncTrackerCollection(Func<IEnumerable<ISyncTrackerBase>> items)
        : base(items)
    { }

    public IEnumerable<ISyncTracker<TObject>> GetTrackers<TObject>()
    {
        return this.Where(x => x is ISyncTracker<TObject> tracker)
            .Select(x => x as ISyncTracker<TObject>);
    }

    public IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, SyncSerializerOptions options)
    {
        var changes = new List<uSyncChange>();
        foreach (var tracker in GetTrackers<TObject>())
        {
            changes.AddRange(tracker.GetChanges(node, options));
        }
        return changes;
    }

    public IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, XElement currentNode, SyncSerializerOptions options)
    {
        if (currentNode == null)
            return GetChanges<TObject>(node, options);

        var changes = new List<uSyncChange>();
        foreach (var tracker in GetTrackers<TObject>())
        {
            changes.AddRange(tracker.GetChanges(node, currentNode, options));
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
