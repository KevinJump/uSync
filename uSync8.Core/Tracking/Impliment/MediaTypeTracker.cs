using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class MediaTypeTracker : ContentTypeBaseTracker<IMediaType>, ISyncTracker<IMediaType>
    {
        public MediaTypeTracker(ISyncSerializer<IMediaType> serializer) : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            var tracker = base.TrackChanges();
            tracker.Children[0]
                .Children.Add(new TrackedItem("Folder", "/Folder", true));

            tracker.Children.Add(
                    new TrackedItem("Structure", "/Structure")
                    {
                        Children = new List<TrackedItem>()
                        {
                            new TrackedItem("MediaType", "/MediaType")
                            {
                                Repeating = new RepeatingInfo(string.Empty, string.Empty, string.Empty)
                            }
                        }
                    });

            return tracker;
        }
    }
}
