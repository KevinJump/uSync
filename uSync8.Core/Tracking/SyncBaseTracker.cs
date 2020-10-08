using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        private const string seperator = " > "; 

        protected readonly ISyncSerializer<TObject> serializer;

        public SyncBaseTracker(ISyncSerializer<TObject> serializer)
        {
            this.serializer = serializer;
        }

        protected abstract TrackedItem TrackChanges();

        public virtual IEnumerable<uSyncChange> GetChanges(XElement node)
            => GetChanges(node, new SyncSerializerOptions());

        public virtual IEnumerable<uSyncChange> GetChanges(XElement node, SyncSerializerOptions options)
        {
            XElement current = null;

            var item = serializer.FindItem(node);
            if (item != null)
            {
                var attempt = SerializeItem(item, options);
                if (attempt.Success)
                {
                    current = attempt.Item;
                }
            }
            return GetChanges(node, current, options);
        }

        public virtual IEnumerable<uSyncChange> GetChanges(XElement node, XElement current, SyncSerializerOptions options)
        { 
            if (serializer.IsEmpty(node))
            {
                return GetEmptyFileChanges(node, current).AsEnumerableOfOne();
            }

            if (!serializer.IsValid(node))
            {
                // not valid 
                return uSyncChange.Error("", "Invalid File", node.Name.LocalName).AsEnumerableOfOne();
            }


            if (GetFileChange(node, current, options) == ChangeType.NoChange)
            {
                return uSyncChange.NoChange("", node.GetAlias()).AsEnumerableOfOne();
            }

            var changes = TrackChanges();
            if (current != null)
            {
                return CalculateChanges(changes, current, node, "", "");
            }

            return Enumerable.Empty<uSyncChange>();
        }

        private ChangeType GetFileChange(XElement node, XElement current, SyncSerializerOptions options)
        {
            switch (serializer)
            {
                case ISyncNodeSerializer<TObject> nodeSerializer:
                    return nodeSerializer.IsCurrent(node, current, options);
                case ISyncOptionsSerializer<TObject> optionSerializer:
                    return optionSerializer.IsCurrent(node, options);
                default:
                    return serializer.IsCurrent(node);
            }
        }

        private uSyncChange GetEmptyFileChanges(XElement node, XElement current)
        {
            if (!serializer.IsEmpty(node))
                throw new ArgumentException("Cannot calculate empty changes on a non empty file");

            if (current == null) return uSyncChange.NoChange("", node.GetAlias());

            var action = node.Attribute("Change").ValueOrDefault<SyncActionType>(SyncActionType.None);

            switch (action)
            {
                case SyncActionType.Delete:
                    return uSyncChange.Delete(node.GetAlias(), "Delete", node.GetAlias());
                case SyncActionType.Rename:
                    return uSyncChange.Update(node.GetAlias(), "Rename", node.GetAlias(), "new name");
                default:
                    return uSyncChange.NoChange("", node.GetAlias());
            }
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
                    if (targetNode != null)
                    {
                        return uSyncChange.Create(path, name, targetNode.ValueOrDefault(string.Empty), change.CompareValue)
                            .AsEnumerableOfOne();
                    }

                    // if both are null, just return nothing.
                    return updates;
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
                        targetNode.ValueOrDefault(string.Empty),
                        change.MaskValue));
                }

                if (change.Attributes != null && change.Attributes.Any())
                {
                    foreach (var attribute in change.Attributes)
                    {
                        var currentValue = currentNode.Attribute(attribute).ValueOrDefault(string.Empty);
                        var targetValue = targetNode.Attribute(attribute).ValueOrDefault(string.Empty);
                        updates.AddNotNull(Compare(path, $"{name} [{attribute}]", currentValue, targetValue, change.MaskValue));
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

            var currentIndex = 0;
            // loop through the nodes in the current item 
            foreach (var currentNode in currentItems)
            {
                var currentNodePath = path;
                var currentNodeName = name;

                XElement targetNode = null;


                // if the key is blank we just compare the values in the elements
                if (string.IsNullOrWhiteSpace(change.Repeating.Key))
                {
                    if (change.Repeating.ElementsInOrder)
                    {
                        if (targetItems.Count() > currentIndex)
                            targetNode = targetItems.ElementAt(currentIndex);
                    }
                    else
                    {
                        // if the element isn't the key, then we get the first one (by value)
                        // if the value is different in this case we will consider this a delete
                        targetNode = targetItems.FirstOrDefault(x => x.Value == currentNode.Value);
                    }

                }
                else
                {
                    // we need to find the current key value 
                    var currentKey = GetKeyValue(currentNode, change.Repeating.Key, change.Repeating.KeyIsAttribute);
                    if (currentKey == string.Empty) continue;

                    // now we need to make the XPath for the children this will be [key = ''] or [@key =''] 
                    // depending if its an attribute or element key
                    currentNodePath += MakeKeyPath(change.Repeating.Key, currentKey, change.Repeating.KeyIsAttribute);
                    
                    // now see if we can find that node in the target elements we have loaded 
                    targetNode = GetTarget(targetItems, change.Repeating.Key, currentKey, change.Repeating.KeyIsAttribute);

                    if (targetNode == null && !string.IsNullOrWhiteSpace(change.Repeating.Key2))
                    {
                        // we couldn't find it, but we have a second key to look up, so lets do that. 
                        currentKey = GetKeyValue(currentNode, change.Repeating.Key2, change.Repeating.Key2IsAttribute);
                        if (currentKey == string.Empty) continue;
                        currentNodePath = path + MakeKeyPath(change.Repeating.Key2, currentKey, change.Repeating.Key2IsAttribute);

                        targetNode = GetTarget(targetItems, change.Repeating.Key2, currentKey, change.Repeating.Key2IsAttribute);
                    }

                    // make the name 
                    if (!string.IsNullOrWhiteSpace(change.Repeating.Name))
                    {
                        var itemName = GetKeyValue(currentNode, change.Repeating.Name, change.Repeating.NameIsAttribute);
                        if (!string.IsNullOrWhiteSpace(itemName))
                        {
                            currentNodeName += $"{seperator}{itemName}";
                        }
                    }
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
                        targetNode.ValueOrDefault(string.Empty),
                        change.MaskValue));
                }

                currentIndex++;
            }

            if (!change.Repeating.ElementsInOrder)
            {
                // look for things in target but not current (for they will be removed)
                List<XElement> missing = new List<XElement>();

                if (string.IsNullOrWhiteSpace(change.Repeating.Key))
                {
                    missing = targetItems.Where(x => !currentItems.Any(t => t.Value == x.Value))
                        .ToList();
                }
                else
                {
                    foreach (var targetItem in targetItems)
                    {
                        var targetNodePath = path;

                        var targetKey = GetKeyValue(targetItem, change.Repeating.Key, change.Repeating.KeyIsAttribute);
                        if (string.IsNullOrEmpty(targetKey)) continue;

                        targetNodePath += MakeKeyPath(change.Repeating.Key, targetKey, change.Repeating.KeyIsAttribute);
                        var currentNode = GetTarget(currentItems, change.Repeating.Key, targetKey, change.Repeating.KeyIsAttribute);
                        if (currentNode == null && !string.IsNullOrWhiteSpace(change.Repeating.Key2))
                        {
                            var targetKey2 = GetKeyValue(targetItem, change.Repeating.Key2, change.Repeating.Key2IsAttribute);
                            currentNode = GetTarget(currentItems, change.Repeating.Key2, targetKey2, change.Repeating.Key2IsAttribute);
                        }

                        if (currentNode == null)
                        {
                            missing.Add(targetItem);
                        }
                    }
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
                    updates.Add(uSyncChange.Delete(path, $"{name} [{childNode.Name.LocalName}]", GetElementValues(childNode)));
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
                    var childValue = childNode.ValueOrDefault(string.Empty);
                    var targetChildValue = targetChildNode.ValueOrDefault(string.Empty);

                    if (!childValue.Equals(targetChildValue))
                    {
                        // if there are no children, they we are comparing the actual text of the nodes
                        updates.AddNotNull(Compare(currentNodePath, currentNodeName,
                            childValue, targetChildValue,
                            change.MaskValue));
                    }
                }
            }

            // missing from current (so new)
            foreach (var targetChild in targetNode.Elements())
            {
                var currentChildNode = currentNode.Element(targetChild.Name.LocalName);
                if (currentChildNode == null)
                {
                    // not in current, its a new property.
                    updates.Add(uSyncChange.Create(path, $"{name} [{targetChild.Name.LocalName}]", GetElementValues(targetChild)));
                }
            }
            return updates;
        }

        /// <summary>
        ///  combines all the possible values from child nodes into a comma seperated list.
        /// </summary>
        /// <returns>list of values or (blank)</returns>
        private string GetElementValues(XElement node)
        {
            var value = string.Empty;
            if (node != null)
                value = string.Join(",", node.Elements().Select(x => x.Value));

            if (string.IsNullOrWhiteSpace(value)) value = "(blank)";
            return value;
        }

        protected uSyncChange Compare(string path, string name, string current, string target, bool maskValue)
        {
            if (current.DetectIsJson())
            {
                return JsonChange(path, name, current, target, maskValue);
            }
            else
            {
                return StringChange(path, name, current, target, maskValue);
            }
        }

        private uSyncChange StringChange(string path, string name, string current, string target, bool maskValue)
        {
            if (current.Equals(target)) return null;
            return uSyncChange.Update(path, name,
                maskValue ? "*******" : current,
                maskValue ? "*******" : target);
        }

        private uSyncChange JsonChange(string path, string name, string current, string target, bool maskValue)
        {
            try
            {
                // jsoncompare. for sanity, we serialize and deserialize into
                var currentJson = JsonConvert.DeserializeObject<JToken>(current);
                var targetJson = JsonConvert.DeserializeObject<JToken>(target);

                if (JToken.DeepEquals(currentJson, targetJson)) return null;

                return uSyncChange.Update(path, name, currentJson, targetJson);
            }
            catch
            {
                return StringChange(path, name, current, target, maskValue);
            }
        }

        private uSyncChange Compare<TValue>(string path, string name, TValue current, TValue target, bool maskValue)
        {
            if (current.Equals(target)) return null;
            return Compare(path, name, current.ToString(), target.ToString(), maskValue);
        }

        private string GetChangePath(string path, string child)
        {
            return path + child.Replace("//", "/");
        }

        private string GetChangeName(string parent, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return parent;

            if (!string.IsNullOrWhiteSpace(parent))
                return $"{parent}{seperator}{name}";

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


        private SyncAttempt<XElement> SerializeItem(TObject item, SyncSerializerOptions options)
        {
            if (serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                return optionSerializer.Serialize(item, options);

#pragma warning disable CS0618 // Type or member is obsolete
            return serializer.Serialize(item);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }


    public class TrackedItem
    {
        private TrackedItem(string name)
        {
            Name = name;
            Path = "/";

            Attributes = new List<string>
            {
                "Key",
                "Alias"
            };
        }

        public TrackedItem(string name, bool root)
            : this(name)
        {
            // if (root == true)
            // {
            // }
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

        public bool MaskValue { get; set; }
    }

    public class RepeatingInfo
    {
        public RepeatingInfo(string key, string value, string name)
        {
            Key = key;
            Value = value;
            Name = name;
        }

        public RepeatingInfo(string key, string key2, string value, string name) 
            : this(key, value, name)
        {
            Key2 = key2;
        }

        /// <summary>
        ///  Element used to match items in a collection of nodes
        ///  (e.g Key)
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///  secondary element used to match items (e.g if key fails, we check alias)
        /// </summary>
        public string Key2 { get; set; }
        

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

        public bool Key2IsAttribute { get; set; }

        public bool NameIsAttribute { get; set; }

        /// <summary>
        ///  when the element is the key, so if the value doesn't match
        ///  that just means the value has been updated (not deleted)
        /// </summary>
        public bool ElementsInOrder { get; set; }
    }
}
