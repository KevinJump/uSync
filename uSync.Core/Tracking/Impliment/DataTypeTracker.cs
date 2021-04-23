using System.Collections.Generic;

using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class DataTypeTracker : SyncXmlTracker<IDataType>, ISyncTracker<IDataType>
    {
        public DataTypeTracker(ISyncSerializer<IDataType> serializer)
            : base(serializer)
        {
        }

        public override List<TrackingItem> TrackingItems => new List<TrackingItem>()
        {
            TrackingItem.Single("Name", "/Info/Name"),
            TrackingItem.Single("EditorAlias", "/Info/EditorAlias"),
            TrackingItem.Single("Database Type", "/Info/DatabaseType"),
            TrackingItem.Single("Sort Order", "/Info/SortOrder"),
            TrackingItem.Single("Folder", "/Info/Folder"),
            TrackingItem.Single("Config", "/Config")
        };
    }
}
