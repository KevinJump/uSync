using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
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
                            new TrackedItem("SortOrder", "/SortOrder")
                        }
                    },
                    new TrackedItem("Config", "/Config", true)
                }
            };
        }
    }
}
