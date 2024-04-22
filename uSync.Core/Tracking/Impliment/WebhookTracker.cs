using System.Collections.Generic;

using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
	public class WebhookTracker : SyncXmlTrackAndMerger<IWebhook>, ISyncTracker<IWebhook>
	{
		public WebhookTracker(SyncSerializerCollection serializers) : base(serializers)
		{
		}

        public override List<TrackingItem> TrackingItems => [
            TrackingItem.Single("Enabled", "/Enabled"),
			TrackingItem.Many("Events", "/Events/Event", "Event"),
			TrackingItem.Many("Headers", "/Headers/Header", "@Key"),
			TrackingItem.Many("ContentKeys", "/ContentTypeKeys/Key", "Key"),
		];
	}
}
