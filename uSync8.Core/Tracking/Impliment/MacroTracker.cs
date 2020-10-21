using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class MacroTracker : SyncBaseTracker<IMacro>, ISyncNodeTracker<IMacro>
    {
        public MacroTracker(ISyncSerializer<IMacro> serializer) : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            return new TrackedItem(serializer.ItemType, true)
            {
                Children = new List<TrackedItem>()
                {
                    new TrackedItem("Name", "/Name", true),
                    new TrackedItem("Source", "/MacroSource", true),
                    new TrackedItem("Type", "/MacroType", true),
                    new TrackedItem("Use In Editor", "/UseInEditor", true),
                    new TrackedItem("Don't Render in Editor", "/DontRender", true),
                    new TrackedItem("Cache By Member", "/CachedByMember", true),
                    new TrackedItem("Cache By Page", "/CachedByPage", true),
                    new TrackedItem("Cache Duration", "/CachedDuration", true),

                    new TrackedItem("", "/Properties", false)
                    {
                        Children = new List<TrackedItem>()
                        {
                            new TrackedItem("Property", "/Property")
                            {
                                Repeating = new RepeatingInfo("Alias", "/Property", "Name"),
                                Children = new List<TrackedItem>()
                                {
                                    new TrackedItem("Name", "/Name", true),
                                    new TrackedItem("Alias", "/Alias", true),
                                    new TrackedItem("SortOrder", "/SortOrder", true),
                                    new TrackedItem("EditorAlias", "/EditorAlias", true)
                                }
                            }
                        }
                    }
                }

            };
        }
    }
}
