using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class ContentTypeBaseTracker<TObject> : SyncBaseTracker<TObject>
        where TObject : IContentTypeBase
    {
        public ContentTypeBaseTracker(ISyncSerializer<TObject> serializer) : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            return new TrackedItem("", true)
            {
                Children = new List<TrackedItem>()
                {
                    new TrackedItem("", "/Info")
                    {
                        Children = new List<TrackedItem>()
                        {
                            new TrackedItem("Name", "/Name", true),
                            new TrackedItem("Icon", "/Icon", true),
                            new TrackedItem("Thumbnail", "/Thumbnail", true),
                            new TrackedItem("Description", "/Description", true),
                            new TrackedItem("AllowAtRoot", "/AllowAtRoot", true),
                            new TrackedItem("IsListView", "/IsListView", true),
                            new TrackedItem("Variations", "/Variations", true),
                            new TrackedItem("IsElement", "/IsElement", true),
                        }
                    },
                    new TrackedItem("", "/GenericProperties", false)
                    {
                        Children = new List<TrackedItem>()
                        {
                            new TrackedItem("Property", "/GenericProperty")
                            {
                                Repeating = new RepeatingInfo("Key", "Alias", "/GenericProperty", "Name"),
                                Children = new List<TrackedItem>()
                                {
                                    new TrackedItem("Key", "/Key", true),
                                    new TrackedItem("Name", "/Name", true),
                                    new TrackedItem("Alias", "/Alias", true),
                                    new TrackedItem("Definition", "/Definition", true),
                                    new TrackedItem("Type", "/Type", true),
                                    new TrackedItem("Mandatory", "/Mandatory", true),
                                    new TrackedItem("Validation", "/Validation", true),
                                    new TrackedItem("Description", "/Description", true),
                                    new TrackedItem("SortOrder", "/SortOrder", true),
                                    new TrackedItem("Tab", "/Tab", true),
                                    new TrackedItem("Variations", "/Variations", true),
                                    new TrackedItem("MandatoryMessage", "/MandatoryMessage", true),
                                    new TrackedItem("ValidationRegExpMessage", "/ValidationRegExpMessage", true)
                                }
                            }
                        }
                    },
                    new TrackedItem("", "/Tabs", false)
                    {
                        Children = new List<TrackedItem>()
                        {
                            new TrackedItem("Tab", "/Tab")
                            {
                                Repeating = new RepeatingInfo("Caption", "/Tab", "Caption"),
                                Children = new List<TrackedItem>()
                                {
                                    new TrackedItem("Caption", "/Caption", true),
                                    new TrackedItem("SortOrder", "/SortOrder", true)
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
