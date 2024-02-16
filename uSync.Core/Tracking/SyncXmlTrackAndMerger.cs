using System.Xml.Linq;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking;

public class SyncXmlTrackAndMerger<TObject>
    : SyncXmlTracker<TObject>
{
    public SyncXmlTrackAndMerger(SyncSerializerCollection serializers)
        : base(serializers)
    {
    }

    public override XElement MergeFiles(XElement a, XElement b)
        => SyncRootMergerHelper.GetCombined([a, b], TrackingItems);

    public override XElement GetDifferences(List<XElement> nodes)
        => SyncRootMergerHelper.GetDifferences(nodes, TrackingItems);

}
