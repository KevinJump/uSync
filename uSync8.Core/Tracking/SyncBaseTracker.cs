using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Umbraco.Core;
using Umbraco.Core.Models.Entities;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking
{
    public abstract class SyncBaseTracker<TObject> 
        where TObject : IEntity
    {
        protected readonly ISyncSerializer<TObject> serializer;

        public SyncBaseTracker(ISyncSerializer<TObject> serializer)
        {
            this.serializer = serializer;
        }

        protected abstract TrackedItem TrackChanges();

        public virtual IEnumerable<uSyncChange> GetChanges(XElement node)
        {
            if (!serializer.IsValid(node))
                return Enumerable.Empty<uSyncChange>();

            if (serializer.IsCurrent(node))
            {
                return new uSyncChange()
                {
                    Change = ChangeDetailType.NoChange,
                    Name = node.GetAlias(),
                }.AsEnumerableOfOne();
            }

            var changes = TrackChanges();

            var item = serializer.GetItem(node);
            if (item != null) {
                var current = serializer.Serialize(item);
                if (current.Success)
                {
                    return CalculateChanges(changes, current.Item, node);
                }
            }

            return Enumerable.Empty<uSyncChange>();
        }


        private IEnumerable<uSyncChange> CalculateChanges(TrackedItem change, XElement current, XElement target)
        {
            var updates = new List<uSyncChange>();

            if (change == null) return Enumerable.Empty<uSyncChange>();

            if (string.IsNullOrWhiteSpace(change.Name) || string.IsNullOrWhiteSpace(change.Path))
                return Enumerable.Empty<uSyncChange>();

            var currentNode = current.XPathSelectElement(change.Path);
            var targetNode = target.XPathSelectElement(change.Path);

            if (currentNode == null)
            {
                updates.Add(new uSyncChange()
                {
                    Change = ChangeDetailType.Create,
                    Path = change.Path,
                    Name = change.Name,
                    OldValue = change.CompareValue ? targetNode.ValueOrDefault(string.Empty) : "New Property",
                    NewValue = ""
                });
            }

            if (targetNode == null)
            {
                // its a delete (not in target)
                updates.Add(new uSyncChange()
                {
                    Change = ChangeDetailType.Delete,
                    Path = change.Path,
                    Name = change.Name,
                    OldValue = change.CompareValue ? currentNode.ValueOrDefault(string.Empty) : "Missing Property",
                    NewValue = ""
                });
            }

            if (change.CompareValue)
            {
                // actual change 
                updates.AddNotNull(Compare(change.Name, change.Path,
                    currentNode.ValueOrDefault(string.Empty),
                    targetNode.ValueOrDefault(string.Empty)));
            }

            if (change.Attributes != null && change.Attributes.Any())
            {
                foreach(var attribute in change.Attributes)
                {
                    var currentValue = currentNode.Attribute(attribute).ValueOrDefault(string.Empty);
                    var targetValue = targetNode.Attribute(attribute).ValueOrDefault(string.Empty);
                    updates.AddNotNull(Compare($"{change.Name} [{attribute}]", change.Path, currentValue, targetValue));
                }
            }

            if (change.Children != null && change.Children.Any())
            {
                foreach(var child in change.Children)
                {
                    updates.AddRange(CalculateChanges(child, current, target));
                }
            }

            return updates;
        }
  

        protected uSyncChange Compare(string name, string path, string current, string target)
        {
            if (current == target) return null;

            return new uSyncChange()
            {
                Name = name,
                Path = path,
                Change = ChangeDetailType.Update,
                NewValue = target,
                OldValue = current
            };
        }

        private uSyncChange Compare<TValue>(string name, string path, TValue current, TValue target)
        {
            if (current.Equals(target)) return null;

            return Compare(name, path, current.ToString(), target.ToString());
        }

        protected virtual bool XmlMatches(XElement oldNode, XElement newNode)
        {
            return false;
        }
    }


    public class TrackedItem
    {
        public TrackedItem(string name)
        {
            Name = name;
            Path = "/";

            Attributes = new List<string>
            {
                "Key",
                "Alias"
            };

            Children = new List<TrackedItem>();
        }

        public TrackedItem(string name, string path)
            : this(name)
        {
            Name = name;
            Path = path;
        }

        public TrackedItem(string name, string path, bool compareValue)
            :this(name, path)
        {
            CompareValue = compareValue;
        }

        public bool CompareValue { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public List<string> Attributes { get; set; }
        public List<TrackedItem> Children { get; set; }
    }
}
