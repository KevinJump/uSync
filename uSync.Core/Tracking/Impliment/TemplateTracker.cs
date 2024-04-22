using System.Collections.Generic;

using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class TemplateTracker : SyncXmlTrackAndMerger<ITemplate>, ISyncTracker<ITemplate>
    {
        public TemplateTracker(SyncSerializerCollection serializers)
            : base(serializers)
        { }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>()
        {
            TrackingItem.Single("Name", "/Name"),
            TrackingItem.Single("Parent", "/Parent"),
            TrackingItem.Single("Contents", "/Contents")
        };
    }
}
