using System.Collections.Generic;

using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class ContentTypeBaseTracker<TObject> : SyncXmlTrackAndMerger<TObject>
        where TObject : IContentTypeBase
    {
        public ContentTypeBaseTracker(SyncSerializerCollection serializers)
            : base(serializers)
        { }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>()
        {
            TrackingItem.Single("Name", "/Info/Name"),
            TrackingItem.Single("Icon", "/Info/Icon"),
            TrackingItem.Single("Thumbnail", "/Info/Thumbnail"),
            TrackingItem.Single("Description", "/Info/Description"),
            TrackingItem.Single("Allowed at root", "/Info/AllowAtRoot"),
            TrackingItem.Single("List View", "/Info/IsListView"),
            TrackingItem.Single("Variations", "/Info/Variations"),
            TrackingItem.Single("Element type", "/Info/IsElement"),
            TrackingItem.Single("Folder", "/Info/Folder"),
            TrackingItem.Single("Default Template", "/Info/DefaultTemplate"),
            
           
            TrackingItem.Single("History", "/Info/HistoryCleanup"),

            TrackingItem.Many("Compositions", "/Info/Compositions/Composition", "@Key"),
            TrackingItem.Many("AllowedTemplates", "/Info/AllowedTemplates/Template", "@Key"),

            TrackingItem.Many("Allowed child node types", "/Structure/ContentType", "@Key"),
            TrackingItem.Many("Allowed child node types", "/Structure/MediaType", "@Key"),
            TrackingItem.Many("Allowed child node types", "/Structure/MemberType", "@Key"),


            TrackingItem.Many("Property", "/GenericProperties/GenericProperty", uSyncConstants.Xml.Key, "Name", "Alias"),
            TrackingItem.Many("Groups", "/Tabs/Tab", "Caption", "Caption")
        };
    }
}
