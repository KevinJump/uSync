using System.Collections.Generic;

using Umbraco.Core.Models;

using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking.Impliment
{
    public class ContentTypeTracker : ContentTypeBaseTracker<IContentType>, ISyncTracker<IContentType>
    {
        public ContentTypeTracker(ISyncSerializer<IContentType> serializer) : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            var tracker = base.TrackChanges();
            tracker.Children[0]
                .Children.Add(new TrackedItem("DefaultTemplate", "/DefaultTemplate", true));

            tracker.Children[0]
                .Children.Add(new TrackedItem("Parent", "/Parent", true));
            tracker.Children[0]
                .Children.Add(new TrackedItem("Folder", "/Folder", true));

            tracker.Children[0]
                .Children.Add(new TrackedItem("AllowedTemplates", "/AllowedTemplates")
                {
                    Children = new List<TrackedItem>()
                    {
                        new TrackedItem("Template", "/Template")
                        {
                            Repeating = new RepeatingInfo("Key", string.Empty, string.Empty)
                            {
                                KeyIsAttribute = true
                            }
                        }
                    }
                });

            tracker.Children[0]
                .Children.Add(new TrackedItem("Compositions", "/Compositions")
                {
                    Children = new List<TrackedItem>()
                    {
                        new TrackedItem("Composition", "/Composition")
                        {
                            Repeating = new RepeatingInfo("Key", string.Empty, string.Empty)
                            {
                                KeyIsAttribute = true
                            }
                        }
                    }
                });

            tracker.Children.Add(
                new TrackedItem("Structure", "/Structure")
                {
                    Children = new List<TrackedItem>()
                    {
                        new TrackedItem("ContentType", "/ContentType")
                        {
                            Repeating = new RepeatingInfo("SortOrder", string.Empty, "Key")
                            {
                                KeyIsAttribute = true,
                                NameIsAttribute = true
                            }
                        }
                    }
                });

            return tracker;
        }
    }
}
