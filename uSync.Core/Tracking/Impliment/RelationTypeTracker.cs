using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class RelationTypeTracker
    : SyncXmlTracker<IRelationType>, ISyncTracker<IRelationType>
{
    public RelationTypeTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }

    public override List<TrackingItem> TrackingItems =>
    [
        TrackingItem.Single("Name", "/Info/Name"),
        TrackingItem.Single("ParentType", "/Info/ParentType"),
        TrackingItem.Single("ChildType", "/Info/ChildType"),
        TrackingItem.Single("Bidirectional", "/Info/Bidirectional")
    ];
}
