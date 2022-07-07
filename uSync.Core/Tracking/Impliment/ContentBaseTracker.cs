using System.Collections.Generic;

using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public abstract class ContentBaseTracker<TObject> : SyncXmlTracker<TObject>
        where TObject : IContentBase
    {
        public ContentBaseTracker(SyncSerializerCollection serializers)
            : base(serializers)
        { }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>()
        {
            TrackingItem.Single("Parent", "/Info/Parent"),
            TrackingItem.Single("Path", "/Info/Path"),
            TrackingItem.Single("Trashed", "/Info/Trashed"),
            TrackingItem.Single("ContentType", "/Info/ContentType"),
            TrackingItem.Single("CreatedDate", "/Info/CreateDate"),
            TrackingItem.Single("SortOrder", "/Info/SortOrder"),
            TrackingItem.Single("Template", "/Info/Template"),
            TrackingItem.Single("FileHash", "/Info/FileHash"),

            TrackingItem.Attribute("NodeName (Default)", "/Info/NodeName", "Default"),
            TrackingItem.Many("Name", "/Info/NodeName/Name", "@Culture"),

            TrackingItem.Attribute("Published (Default)", "/Info/Published", "Default"),
            TrackingItem.Many("Published", "/Info/Published/Published", "@Culture"),

            TrackingItem.Many("Schedule", "/Info/Schedule/ContentSchedule", "Culture,Action", "Date"),

            TrackingItem.Many("Property - *", "/Properties/*/Value", "@Culture"),

            TrackingItem.Many("GenericProperty", "/GenericProperties/GenericProperty", uSyncConstants.Xml.Key)
        };
    }
}
