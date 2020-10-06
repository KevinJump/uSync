using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Tracker
{
    public class RelationTypeTracker
        : SyncBaseTracker<IRelationType>, ISyncNodeTracker<IRelationType>
    {
        public RelationTypeTracker(ISyncSerializer<IRelationType> serializer) : base(serializer)
        { }

        protected override TrackedItem TrackChanges()
        {
            return new TrackedItem(serializer.ItemType, true)
            {
                Children = new List<TrackedItem>()
                {
                    new TrackedItem("Info", "/Info")
                    {
                        Children = new List<TrackedItem>()
                        {
                            new TrackedItem("Name", "/Name", true),
                            new TrackedItem("ParentType", "/ParentType", true),
                            new TrackedItem("ChildType", "/ChildType", true),
                            new TrackedItem("Bidirectional", "/Bidirectional", true)
                        }
                    }
                }
            };
        }
    }
}
