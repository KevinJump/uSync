using System.Xml.Linq;

using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking;

public interface ISyncTrackerBase
{

    XElement MergeFiles(XElement a, XElement b)
        => b;

    XElement? GetDifferences(List<XElement> nodes)
        => nodes.Count > 0 ? nodes[^1] : null;

}

public interface ISyncTracker<TObject> : ISyncTrackerBase
{
    IEnumerable<uSyncChange> GetChanges(XElement node, XElement current, SyncSerializerOptions options);
    IEnumerable<uSyncChange> GetChanges(XElement node, SyncSerializerOptions options);
}
