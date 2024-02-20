using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class DomainTracker : SyncXmlTracker<IDomain>, ISyncTracker<IDomain>
{
    public DomainTracker(SyncSerializerCollection serializers)
        : base(serializers)
    {
    }

    public override List<TrackingItem> TrackingItems => new List<TrackingItem>()
    {
        TrackingItem.Single("Domain > Wildcard", "/Info/IsWildcard"),
        TrackingItem.Single("Domain > Language", "/Info/Language"),
        TrackingItem.Single("Domain > Root", "/Info/Root")
    };
}
