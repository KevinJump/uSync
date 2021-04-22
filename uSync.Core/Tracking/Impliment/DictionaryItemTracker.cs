using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class DictionaryItemTracker : SyncXmlTracker<IDictionaryItem>, ISyncNodeTracker<IDictionaryItem>
    {
        public DictionaryItemTracker(ISyncSerializer<IDictionaryItem> serializer) : base(serializer)
        { }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>()
        {
            TrackingItem.Single("Parent", "/Info/Parent"),
            TrackingItem.Many("Translation", "/Translations/Translation", "@Language")
        };
    }
}
