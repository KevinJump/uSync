using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class LanguageTracker : SyncXmlTracker<ILanguage>, ISyncNodeTracker<ILanguage>
    {
        public LanguageTracker(ISyncSerializer<ILanguage> serializer) : base(serializer)
        {
        }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>()
        {
            TrackingItem.Single("IsoCode", "/IsoCode"),
            TrackingItem.Single("Mandatory", "IsMandatory"),
            TrackingItem.Single("Default Language", "/IsDefault"),
            TrackingItem.Single("CultureName", "/CultureName")
        };
    }
}
