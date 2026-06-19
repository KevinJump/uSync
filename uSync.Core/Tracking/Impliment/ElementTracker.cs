using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class ElementTracker : SyncXmlTrackAndMerger<EntityContainer>, ISyncTracker<EntityContainer>
{
    public ElementTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }

    public override List<TrackingItem> TrackingItems =>
    [
        TrackingItem.Single("Parent", "/Parent"),
        TrackingItem.Single("SortOrder", "/SortOrder")
    ];
}