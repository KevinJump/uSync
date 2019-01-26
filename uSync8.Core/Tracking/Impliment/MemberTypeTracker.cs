using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class MemberTypeTracker : SyncBaseTracker<IMemberType>, ISyncTracker<IMemberType>
    {
        public MemberTypeTracker(ISyncSerializer<IMemberType> serializer) : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            return new TrackedItem(serializer.ItemType);
        }
    }
}
