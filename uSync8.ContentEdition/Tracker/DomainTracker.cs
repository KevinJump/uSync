using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Tracker
{
    public class DomainTracker : SyncXmlTracker<IDomain>, ISyncNodeTracker<IDomain>
    {
        public DomainTracker(ISyncSerializer<IDomain> serializer) : base(serializer)
        {
        }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>()
        {
            TrackingItem.Single("Domain > Wildcard", "/Info/IsWildcard"),
            TrackingItem.Single("Domain > Language", "/Info/Language"),
            TrackingItem.Single("Domain > Root", "/Info/Root")
        };
    }
}
