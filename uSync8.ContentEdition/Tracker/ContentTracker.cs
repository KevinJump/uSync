using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Models;

using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Tracker
{
    public class ContentTracker : SyncBaseTracker<IContent>, ISyncTracker<IContent>
    {
        public ContentTracker(ISyncSerializer<IContent> serializer) : base(serializer)
        {
        }

        protected override TrackedItem TrackChanges()
        {
            return new TrackedItem(serializer.ItemType, true)
            {
                Children = new List<TrackedItem>()
                {
                    new TrackedItem("", "/Info")
                    {
                        Children = new List<TrackedItem>()
                        {
                            new TrackedItem("Parent", "/Parent", true),
                            new TrackedItem("Path", "/Path", true),
                            new TrackedItem("SortOrder", "/SortOrder", true),
                            new TrackedItem("FileHash", "/FileHash", true),
                            new TrackedItem("Template", "/Template", true),
                            new TrackedItem("CreateDate", "/CreateDate", true),

                            new TrackedItem("Name", "/NodeName", false)
                            {
                                Attributes = new List<string>() { "Default" },
                                Children = new List<TrackedItem>()
                                {
                                    new TrackedItem("", "/Name")
                                    {
                                        Repeating = new RepeatingInfo("Culture", string.Empty, "Culture")
                                        {
                                            KeyIsAttribute = true,
                                            NameIsAttribute = true
                                        }
                                    }
                                }
                            },

                            new TrackedItem("Published", "/Published")
                            {
                                Attributes = new List<string>() { "Default" },
                                Children = new List<TrackedItem>()
                                {
                                    new TrackedItem("", "/Published")
                                    {
                                        Repeating = new RepeatingInfo("Culture", string.Empty, "Culture")
                                        {
                                            KeyIsAttribute = true,
                                            NameIsAttribute = true
                                        }
                                    }
                                }
                            },
                            new TrackedItem("Schedule", "/Schedule")
                            {
                                Children = new List<TrackedItem>()
                                {
                                    new TrackedItem("", "/ContentSchedule")
                                    {
                                        Repeating = new RepeatingInfo("Key", "/ContentSchedule", "Date"){
                                            KeyIsAttribute = true
                                        },
                                        Children = new List<TrackedItem>()
                                        {
                                            new TrackedItem("Culture", "/Culture", true),
                                            new TrackedItem("Date", "/Date", true),
                                            new TrackedItem("Action", "/Action", true)
                                        }
                                    }
                                }
                            }
                        }
                    },

                    new TrackedItem("Property", "/Properties")
                    {
                        HasChildProperties = true,
                        Children = new List<TrackedItem>()
                        {
                            new TrackedItem("", "/Value")
                            {
                                Repeating = new RepeatingInfo(string.Empty, string.Empty,string.Empty)
                                {
                                    ElementsInOrder = true
                                }
                            }
                        }
                    }
                }
            };
        }

        protected override SyncSerializerOptions GetSerializerOptions(XElement node)
        {
            var options = base.GetSerializerOptions(node);
            var nodeCultures = node.GetCultures();

            if (!string.IsNullOrEmpty(nodeCultures))
            {
                // workout which of the cultures on the node, are in our settings
                // (if its blank then we mean all)
                var cultureList = nodeCultures.ToDelimitedList();
                var avalibleCultures = options.GetCultures();
                var validCultures = cultureList.Where(x => avalibleCultures.IsValid(x));

                if (validCultures.Any())
                {
                    // make a copy of the options (with a new dictionary of settings).
                    var nodeOptions = new SyncSerializerOptions(options.Flags, options.Settings);
                    nodeOptions.Settings[uSyncConstants.CultureKey] = string.Join(",", validCultures);

                    if (node.Attribute(uSyncConstants.DefaultsKey).ValueOrDefault(false))
                    {
                        nodeOptions.Settings[uSyncConstants.DefaultsKey] = true.ToString();
                    }

                    return nodeOptions;
                }
            }
            return options;
        }

    }
}