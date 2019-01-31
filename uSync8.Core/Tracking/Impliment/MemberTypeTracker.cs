using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class MemberTypeTracker : ContentTypeBaseTracker<IMemberType>, ISyncTracker<IMemberType>
    {
        public MemberTypeTracker(ISyncSerializer<IMemberType> serializer) : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            var tracker = base.TrackChanges();

            // the membership one is a bit of a hack
            // because we want to add new properties in 
            // the repeating properties, but it works.
            var properties = tracker.Children.FirstOrDefault(x => x.Path == "/GenericProperties");
            if (properties != null)
            {
                properties.Children[0]
                    .Children.Add(new TrackedItem("CanEdit", "/CanEdit", true));
                properties.Children[0]
                    .Children.Add(new TrackedItem("CanView", "/CanView", true));
                properties.Children[0]
                    .Children.Add(new TrackedItem("IsSensitive", "/IsSensitive", true));
            }



            return tracker;
        }
    }
}
