using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models;
using uSync.Core.Extensions;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment
{
    public class TemplateTracker : SyncBaseTracker<ITemplate>, ISyncTracker<ITemplate>
    {
        public TemplateTracker(ISyncSerializer<ITemplate> serializer) 
            : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            return new TrackedItem(serializer.ItemType, true)
            {
                Children = new List<TrackedItem>
                {
                    new TrackedItem("Name", "/Name", true),
                    new TrackedItem("Parent", "/Parent", true)
                }
            };
        }
    }
}
