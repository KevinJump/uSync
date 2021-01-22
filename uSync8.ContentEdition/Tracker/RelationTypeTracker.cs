using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Tracker
{
    public class RelationTypeTracker
        : SyncXmlTracker<IRelationType>, ISyncNodeTracker<IRelationType>
    {
        public RelationTypeTracker(ISyncSerializer<IRelationType> serializer) : base(serializer)
        { }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>()
        {
            TrackingItem.Single("Name", "/Info/Name"),
            TrackingItem.Single("ParentType", "/Info/ParentType"),
            TrackingItem.Single("ChildType", "/Info/ChildType"),
            TrackingItem.Single("Bidirectional", "/Info/Bidirectional")
        };
    }
}
