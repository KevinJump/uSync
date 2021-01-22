using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class TemplateTracker : SyncXmlTracker<ITemplate>, ISyncNodeTracker<ITemplate>
    {
        public TemplateTracker(ISyncSerializer<ITemplate> serializer)
            : base(serializer)
        {
        }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>()
        {
            TrackingItem.Single("Name", "/Name"),
            TrackingItem.Single("Parent", "/Parent")
        };
    }
}
