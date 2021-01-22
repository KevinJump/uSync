using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class DataTypeTracker : SyncXmlTracker<IDataType>, ISyncNodeTracker<IDataType>
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
