using System.Xml.Linq;

using uSync.Core.Models;
using uSync.Core.Roots.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking;

public interface ISyncTrackerBase
{

    XElement? MergeFiles(XElement a, XElement b)
        => b;

    XElement? GetDifferences(List<XElement> nodes)
        => nodes.Count > 0 ? nodes[^1] : null;
}

public interface ISyncTrackerOptionsBase : ISyncTrackerBase
{
    XElement? MergeFiles(XElement a, XElement b, SyncFileMergeOptions options);

    XElement? GetDifferences(List<XElement> nodes, SyncFileMergeOptions options);
}


public interface ISyncTracker<TObject> : ISyncTrackerOptionsBase
{
    Task<IEnumerable<uSyncChange>> GetChangesAsync(XElement target, XElement source, SyncSerializerOptions options);
    Task<IEnumerable<uSyncChange>> GetChangesAsync(XElement node, SyncSerializerOptions options);
}
