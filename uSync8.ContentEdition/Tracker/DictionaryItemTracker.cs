using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Tracker
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
