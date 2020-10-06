﻿using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class LanguageTracker : SyncBaseTracker<ILanguage>, ISyncNodeTracker<ILanguage>
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
