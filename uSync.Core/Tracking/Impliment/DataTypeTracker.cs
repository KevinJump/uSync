using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class DataTypeTracker : SyncBaseTracker<IDataType>, ISyncTracker<IDataType>
    {
        public DataTypeTracker(ISyncSerializer<IDataType> serializer) 
            : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            return new TrackedItem(serializer.ItemType, true)
            {
                Children =  new List<TrackedItem>()
                {
                    new TrackedItem("Info", "/Info")
                    {
                        Children = new List<TrackedItem>()
                        {
                            new TrackedItem("Name", "/Name", true),
                            new TrackedItem("EditorAlias", "/EditorAlias", true),
                            new TrackedItem("DatabaseType", "/DatabaseType", true),
                            new TrackedItem("SortOrder", "/SortOrder", true),
                            new TrackedItem("Folder", "/Folder", true)
                        }
                    },
                    new TrackedItem("Config", "/Config", true)
                }
            };
        }
    }
}
