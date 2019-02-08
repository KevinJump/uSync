﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Tracker
{
    public abstract class ContentBaseTracker<TObject> : SyncBaseTracker<TObject>
        where TObject : IContentBase
    {
        public ContentBaseTracker(ISyncSerializer<TObject> serializer)
            : base(serializer)
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
                                Repeating = new RepeatingInfo("Culture", string.Empty, "Culture")
                                {
                                    KeyIsAttribute = true,
                                    NameIsAttribute = true
                                }
                            }
                        }
                    }
                }
            };

        }
    }
}
