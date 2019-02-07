﻿using System.Collections.Generic;
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

            var item = serializer.FindItem(node);
            if (item != null)
            {
                var current = serializer.Serialize(item);
                if (current.Success)
                {
                    return CalculateChanges(changes, current.Item, node, "", "");
                }
            }

            return Enumerable.Empty<uSyncChange>();
        }


        private IEnumerable<uSyncChange> CalculateChanges(TrackedItem change, XElement current, XElement target, string name, string path)
        {
            if (change == null) return Enumerable.Empty<uSyncChange>();

            var changePath = GetChangePath(path, change.Path);
            var changeName = GetChangeName(name, change.Name);

            if (change.Repeating == null)
            {
                if (change.HasChildProperties)
                {
                    return CalculatePropertyChanges(change, current, target, changeName, changePath);
                }
                else
                {
                    return CalculateSingleChange(change, current, target, changeName, changePath);
                }
            }


            return CalculateRepeatingChanges(change, current, target, changeName, changePath);
        }

        private IEnumerable<uSyncChange> CalculateSingleChange(TrackedItem change, XElement current, XElement target, string name, string path)
        {
            var updates = new List<uSyncChange>();

            var currentNode = current;
            var targetNode = target;

            if (!string.IsNullOrEmpty(path))
            {
                currentNode = current.XPathSelectElement(path);
                targetNode = target.XPathSelectElement(path);

                if (currentNode == null)
                {
                    return uSyncChange.Create(path, name, targetNode.ValueOrDefault(string.Empty), change.CompareValue)
                        .AsEnumerableOfOne();

                }

                if (targetNode == null)
                {
                    // its a delete (not in target)
                    return uSyncChange.Delete(path, name, currentNode.ValueOrDefault(string.Empty), change.CompareValue)
                        .AsEnumerableOfOne();
                }

                // this happens if both exist, we compare values in them.

                if (change.CompareValue)
                {
                    // actual change 
                    updates.AddNotNull(Compare(path, name,
                        currentNode.ValueOrDefault(string.Empty),
                        targetNode.ValueOrDefault(string.Empty)));
                }

                if (change.Attributes != null && change.Attributes.Any())
                {
                    foreach (var attribute in change.Attributes)
                    {
                        var currentValue = currentNode.Attribute(attribute).ValueOrDefault(string.Empty);
                        var targetValue = targetNode.Attribute(attribute).ValueOrDefault(string.Empty);
                        updates.AddNotNull(Compare(path, $"{name} [{attribute}]", currentValue, targetValue));
                    }
                }
            }

            if (change.Children != null && change.Children.Any())
            {
                foreach (var child in change.Children)
                {
                    updates.AddRange(CalculateChanges(child, currentNode, targetNode, name, path));
                }
            }

            return updates;
        }

        /// <summary>
        ///  works out changes when we have a repeating block (e.g all the properties on a content type)
        /// </summary>
        private IEnumerable<uSyncChange> CalculateRepeatingChanges(TrackedItem change, XElement current, XElement target, string name, string path)
        {
            var updates = new List<uSyncChange>();

            var currentItems = current.XPathSelectElements(path);
            var targetItems = target.XPathSelectElements(path);

            // loop through the nodes in the current item 
            foreach (var currentNode in currentItems)
            {
                var currentNodePath = path;
                var currentNodeName = name;

                XElement targetNode = null;


                // if the key is blank we just compare the values in the elements
                if (string.IsNullOrWhiteSpace(change.Repeating.Key))
                {
                    targetNode = targetItems.FirstOrDefault(x => x.Value == currentNode.Value);
                }
                else
                {
                    // we need to find the current key value 
                    var currentKey = GetKeyValue(currentNode, change.Repeating.Key, change.Repeating.KeyIsAttribute);
                    if (currentKey == string.Empty) continue;

                    // now we need to make the XPath for the children this will be [key = ''] or [@key =''] 
                    // depending if its an attribute or element key
                    currentNodePath += MakeKeyPath(change.Repeating.Key, currentKey, change.Repeating.KeyIsAttribute);
                    if (!string.IsNullOrWhiteSpace(change.Repeating.Name))
                    {
                        var itemName = GetKeyValue(currentNode, change.Repeating.Name, change.Repeating.NameIsAttribute);
                        if (!string.IsNullOrWhiteSpace(itemName))
                        {
                            currentNodeName += $": {itemName}";
                        }
                    }

                    // now see if we can find that node in the target elements we have loaded 
                    targetNode = GetTarget(targetItems, change.Repeating.Key, currentKey, change.Repeating.KeyIsAttribute);
                }

                if (targetNode == null)
                {
                    // no target, this element will get deleted
                    var oldValue = currentNode.Value;
                    if (!string.IsNullOrWhiteSpace(change.Repeating.Name))
                    {
                        oldValue = GetKeyValue(currentNode, change.Repeating.Name, change.Repeating.NameIsAttribute);
                    }

                    updates.Add(uSyncChange.Delete(path, name, oldValue));
                    continue;
                }

                // check all the children of the current and target for changes 
                if (change.Children != null && change.Children.Any())
                {
                    foreach (var child in change.Children)
                    {
                        updates.AddRange(CalculateChanges(child, currentNode, targetNode, currentNodeName, currentNodePath));
                    }
                }
                else
                {
                    // if there are no children, they we are comparing the actual text of the nodes
                    updates.AddNotNull(Compare(currentNodePath, currentNodeName, 
                        currentNode.ValueOrDefault(string.Empty),
                        targetNode.ValueOrDefault(string.Empty)));
                }
            }

            // look for things in target but not current (for they will be removed)
            List<XElement> missing = new List<XElement>();

            if (string.IsNullOrWhiteSpace(change.Repeating.Key))
            {
                missing = targetItems.Where(x => !currentItems.Any(t => t.Value == x.Value))
                    .ToList();
            }
            else
            {
                missing = targetItems.Where(x =>
                !currentItems.Any(t => t.Element(change.Repeating.Key).ValueOrDefault(string.Empty) == x.Element(change.Repeating.Key).ValueOrDefault(string.Empty)))
                .ToList();
            }

            if (missing.Any())
            {
                foreach (var missingItem in missing)
                {
                    var oldValue = missingItem.Value;
                    if (!string.IsNullOrWhiteSpace(change.Repeating.Name))
                        oldValue = GetKeyValue(missingItem, change.Repeating.Name, change.Repeating.NameIsAttribute);

                    updates.Add(uSyncChange.Create(path, name, oldValue));
                }
            }

            return updates;
        }

        /// <summary>
        ///  works out changes, when the child elements are all properties (and have their own node names)
        ///  this only works when each property is unique (no duplicates in a list)
        /// </summary>
        private IEnumerable<uSyncChange> CalculatePropertyChanges(TrackedItem change, XElement current, XElement target, string name, string path)
        {
            var updates = new List<uSyncChange>();

            var currentNode = current.XPathSelectElement(path);
            var targetNode = target.XPathSelectElement(path);

            foreach (var childNode in currentNode.Elements())
            {
                var currentNodePath = GetChangePath(change.Path, $"/{childNode.Name.LocalName}");
                var currentNodeName = GetChangeName(change.Name, childNode.Name.LocalName);

                // we basically compare to target now.
                var targetChildNode = targetNode.Element(childNode.Name.LocalName);

                if (targetChildNode == null)
                {
                    // no target, this element will get deleted
                    var oldValue = childNode.Name.LocalName;
                    updates.Add(uSyncChange.Delete(path, name, oldValue));
                    continue;
                }

                // check all the children of the current and target for changes 
                if (change.Children != null && change.Children.Any())
                {
                    foreach (var child in change.Children)
                    {
                        updates.AddRange(CalculateChanges(child, currentNode, targetNode, currentNodeName, currentNodePath));
                    }
                }
                else
                {
                    // if there are no children, they we are comparing the actual text of the nodes
                    updates.AddNotNull(Compare(currentNodePath, currentNodeName,
                        childNode.ValueOrDefault(string.Empty),
                        targetChildNode.ValueOrDefault(string.Empty)));
                }
            }

            return updates;
        }

        protected uSyncChange Compare(string path, string name, string current, string target)
        {
            if (current == target) return null;
            return uSyncChange.Update(path, name, current, target);
        }

        private uSyncChange Compare<TValue>(string path, string name, TValue current, TValue target)
        {
            if (current.Equals(target)) return null;
            return Compare(path, name, current.ToString(), target.ToString());
        }

        private string GetChangePath(string path, string child)
        {
            return path + child.Replace("//", "/");
        }

        private string GetChangeName(string parent, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return parent;

            if (!string.IsNullOrWhiteSpace(parent))
                return $"{parent}: " + name;

            return name;
        }

        private string GetKeyValue(XElement node, string key, bool isAttribute)
        {
            if (isAttribute)
            {
                return node.Attribute(key).ValueOrDefault(string.Empty);
            }

            return node.Element(key).ValueOrDefault(string.Empty);
        }

        private string MakeKeyPath(string key, string keyValue, bool isAttribute)
        {
            if (isAttribute)
            {
                return $"[@{key} = '{keyValue}']";
            }

            return $"[{key} = '{keyValue}']";
        }

        private XElement GetTarget(IEnumerable<XElement> items, string key, string value, bool isAttribute)
        {
            if (isAttribute)
                return items.FirstOrDefault(x => x.Attribute(key).ValueOrDefault(string.Empty) == value);

            return items.FirstOrDefault(x => x.Element(key).ValueOrDefault(string.Empty) == value);
        }
    }


    public class TrackedItem
    {
        private TrackedItem(string name)
        {
            Name = name;
            Path = "/";
        }

        public TrackedItem(string name, bool root)
            : this(name)
        {
            if (root == true)
            {
                Attributes = new List<string>
                {
                    "Key",
                    "Alias"
                };
            }
        }

        public TrackedItem(string name, string path)
            : this(name)
        {
            Name = name;
            Path = path;
        }

        public TrackedItem(string name, string path, bool compareValue)
            : this(name, path)
        {
            CompareValue = compareValue;
        }

        public RepeatingInfo Repeating { get; set; }

        public bool CompareValue { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public List<string> Attributes { get; set; } = new List<string>();
        public List<TrackedItem> Children { get; set; } = new List<TrackedItem>();

        public bool HasChildProperties { get; set; }
    }

    public class RepeatingInfo
    {
        public RepeatingInfo(string key, string value, string name)
        {
            Key = key;
            Value = value;
            Name = name;
        }

        /// <summary>
        ///  Element used to match items in a collection of nodes
        ///  (e.g Key)
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///  The repeating element name 
        ///  (e.g GenericProperty)
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        ///  the node we use to display the name of any item in the 
        ///  repeater (e.g Alias)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// indicates if the key is actually an attribute on the node.
        /// </summary>
        public bool KeyIsAttribute { get; set; }

        public bool NameIsAttribute { get; set; }
    }
}
