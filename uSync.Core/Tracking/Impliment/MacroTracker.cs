using System.Collections.Generic;

using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class MacroTracker : SyncXmlTracker<IMacro>, ISyncTracker<IMacro>
    {
        public MacroTracker(ISyncSerializer<IMacro> serializer) : base(serializer)
        {
        }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>()
        {
            TrackingItem.Single("Name", "/Name"),
            TrackingItem.Single("Source", "/MacroSource"),
            TrackingItem.Single("Type", "/MacroType"),
            TrackingItem.Single("Use In Editor", "/UseInEditor"),
            TrackingItem.Single("Don't Render in Editor", "/DontRender"),
            TrackingItem.Single("Cache By Member", "/CachedByMember"),
            TrackingItem.Single("Cache By Page", "/CachedByPage"),
            TrackingItem.Single("Cache Duration", "/CachedDuration"),

            TrackingItem.Many("Property", "/Properties/Property", "Alias", "Alias")
        };
    }
}
