using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class LanguageTracker : SyncXmlTracker<ILanguage>, ISyncTracker<ILanguage>
{
    public LanguageTracker(SyncSerializerCollection serializers)
        : base(serializers)
    {
    }

    public override List<TrackingItem> TrackingItems =>
    [
        TrackingItem.Single("IsoCode", "/IsoCode"),
        TrackingItem.Single("Mandatory", "IsMandatory"),
        TrackingItem.Single("Default Language", "/IsDefault"),
        TrackingItem.Single("CultureName", "/CultureName")
    ];
}
