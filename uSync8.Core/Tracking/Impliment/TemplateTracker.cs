using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class TemplateTracker : SyncBaseTracker<ITemplate>, ISyncTracker<ITemplate>
    {
        public TemplateTracker(ISyncSerializer<ITemplate> serializer)
            : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            return new TrackedItem(serializer.ItemType, true)
            {
                Children = new List<TrackedItem>
                {
                    new TrackedItem("Name", "/Name", true),
                    new TrackedItem("Parent", "/Parent", true)
                }
            };
        }
    }
}
