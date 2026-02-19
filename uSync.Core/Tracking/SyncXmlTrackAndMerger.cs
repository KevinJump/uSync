using System.Xml.Linq;
using uSync.Core.Roots.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking;

public class SyncXmlTrackAndMerger<TObject>
    : SyncXmlTracker<TObject>
{
    public SyncXmlTrackAndMerger(SyncSerializerCollection serializers)
        : base(serializers)
    {
    }

    public virtual XElement? MergeFiles(XElement a, XElement b, SyncFileMergeOptions options)
    {
        if (options.MergeStrategy == SyncMergeStrategy.None)
            return base.MergeFiles(a, b);

        return SyncRootMergerHelper.GetCombined([a, b], TrackingItems, options);
    }

    public virtual XElement? GetDifferences(List<XElement> nodes, SyncFileMergeOptions options)
    {
        if (options.MergeStrategy == SyncMergeStrategy.None)
            return base.GetDifferences(nodes);

        return SyncRootMergerHelper.GetDifferences(nodes, TrackingItems, options);
    }

}
