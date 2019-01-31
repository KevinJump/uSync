using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class LanguageTracker : SyncBaseTracker<ILanguage>, ISyncTracker<ILanguage>
    {
        public LanguageTracker(ISyncSerializer<ILanguage> serializer) : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            return new TrackedItem(serializer.ItemType, true)
            {
                Children = new List<TrackedItem>()
                {
                    new TrackedItem("IsoCode", "/IsoCode", true),
                    new TrackedItem("CultureName", "/CultureName", true),
                    new TrackedItem("Mandatory", "/IsMandatory", true),
                    new TrackedItem("Default Lanaguage", "/IsDefault", true),
                }
            };
        }
    }
}
