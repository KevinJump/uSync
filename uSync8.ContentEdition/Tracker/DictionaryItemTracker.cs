using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Tracker
{
    public class DictionaryItemTracker : SyncBaseTracker<IDictionaryItem>, ISyncTracker<IDictionaryItem>
    {
        public DictionaryItemTracker(ISyncSerializer<IDictionaryItem> serializer) : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            return new TrackedItem(serializer.ItemType, true)
            {
                Children = new List<TrackedItem>()
                {
                    new TrackedItem("Info", "/Info")
                    {
                        Children = new List<TrackedItem>()
                        {
                            new TrackedItem("Parent", "/Parent", true)
                        }
                    },
                    new TrackedItem("Translations", "/Translations")
                    {
                        Repeating = new RepeatingInfo("Language", string.Empty, string.Empty)
                        {
                            KeyIsAttribute = true
                        }
                    }
                }
            };
        }
    }
}
