using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class DictionaryItemTracker : SyncXmlTracker<IDictionaryItem>, ISyncTracker<IDictionaryItem>
{
    public DictionaryItemTracker(SyncSerializerCollection serializers)
        : base(serializers)
    { }

    public override List<TrackingItem> TrackingItems =>
    [
        TrackingItem.Single("Parent", "/Info/Parent"),
        TrackingItem.Many("Translation", "/Translations/Translation", "@Language")
    ];
}
