using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Core;

using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.Core.Tracking
{
    /// <summary>
    ///  tracks the diffrences in the xml between two items. 
    /// </summary>
    public class SyncXmlTracker<TObject>
    {
        protected readonly ISyncSerializer<TObject> serializer;

        private const string seperator = "/";

        public SyncXmlTracker(ISyncSerializer<TObject> serializer)
        {
            this.serializer = serializer;
        }

        public IList<TrackingItem> Items { get; set; }

        public IEnumerable<uSyncChange> GetChanges(XElement target)
            => GetChanges(target, new SyncSerializerOptions());

        public IEnumerable<uSyncChange> GetChanges(XElement target, SyncSerializerOptions options)
        {
            var item = serializer.FindItem(target);
            if (item != null)
            {
                var attempt = SerializeItem(item, options);
                if (attempt.Success)
                    return GetChanges(target, attempt.Item, options);

            }
            return GetChanges(target, null, options);
        }

        private SyncAttempt<XElement> SerializeItem(TObject item, SyncSerializerOptions options)
        {
            if (serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                return optionSerializer.Serialize(item, options);

#pragma warning disable CS0618 // Type or member is obsolete
            return serializer.Serialize(item);
#pragma warning restore CS0618 // Type or member is obsolete
        }


        public IEnumerable<uSyncChange> GetChanges(XElement target, XElement source, SyncSerializerOptions options)
        {
            if (serializer.IsEmpty(target))
                return GetEmptyFileChange(target, source).AsEnumerableOfOne();

            if (!serializer.IsValid(target))
                return uSyncChange.Error("", "Invalid File", target.Name.LocalName).AsEnumerableOfOne();

            var changeType = GetChangeType(target, source, options);
            if (changeType == ChangeType.NoChange)
                return uSyncChange.NoChange("", target.GetAlias()).AsEnumerableOfOne();

            return CalculateDiffrences(target, source);
        }

        private uSyncChange GetEmptyFileChange(XElement target, XElement source)
        {
            if (source == null) return uSyncChange.NoChange("", target.GetAlias());

            var action = target.Attribute("Change").ValueOrDefault(SyncActionType.None);
            switch (action)
            {
                case SyncActionType.Delete:
                    return uSyncChange.Delete(target.GetAlias(), "Delete", target.GetAlias());
                case SyncActionType.Rename:
                    return uSyncChange.Update(target.GetAlias(), "Rename", target.GetAlias(), "New name");
                default:
                    return uSyncChange.NoChange("", target.GetAlias());
            }
        }

        private ChangeType GetChangeType(XElement target, XElement source, SyncSerializerOptions options)
        {
            switch (serializer)
            {
                case ISyncNodeSerializer<TObject> nodeSerializer:
                    return nodeSerializer.IsCurrent(target, source, options);
                case ISyncOptionsSerializer<TObject> optionSerializer:
                    return optionSerializer.IsCurrent(target, options);
                default:
                    return serializer.IsCurrent(target);
            }
        }


        /// <summary>
        ///  actually kicks off here, if you have two xml files that are diffrent. 
        /// </summary>

        private IEnumerable<uSyncChange> CalculateDiffrences(XElement target, XElement source)
        {
            var changes = new List<uSyncChange>();

            foreach (var trackingItem in this.TrackingItems)
            {
                if (trackingItem.SingleItem)
                {
                    changes.AddNotNull(TrackSingleItem(trackingItem, target, source));
                }
                else
                {
                    changes.AddRange(TrackMultipleKeyedItems(trackingItem, target, source));
                }
            }

            return changes;
        }

        private uSyncChange TrackSingleItem(TrackingItem item, XElement target, XElement source)
        {
            var sourceNode = source.XPathSelectElement(item.Path);
            var targetNode = target.XPathSelectElement(item.Path);

            if (sourceNode != null)
            {
                if (targetNode == null)
                {
                    // missing it's a delete.
                    return uSyncChange.Delete(item.Path, item.Name, sourceNode.ValueOrDefault(string.Empty));
                }
                else
                {
                    if (item.HasAttributes())
                    {
                        return Compare(targetNode.Attribute(item.AttributeKey).ValueOrDefault(string.Empty),
                            sourceNode.Attribute(item.AttributeKey).ValueOrDefault(string.Empty),
                            item.Path, item.Name, item.MaskValue);
                    }
                    else
                    {
                        return Compare(targetNode.ValueOrDefault(string.Empty),
                            sourceNode.ValueOrDefault(string.Empty),
                            item.Path, item.Name, item.MaskValue);
                    }
                }
            }

            return null;

        }

        private IEnumerable<uSyncChange> TrackMultipleKeyedItems(TrackingItem trackingItem, XElement target, XElement source)
        {
            var changes = new List<uSyncChange>();

            var sourceItems = source.XPathSelectElements(trackingItem.Path);

            foreach (var sourceNode in sourceItems)
            {
                // make the selection path for this item.
                var itemPath = trackingItem.Path.Replace("*", sourceNode.Parent.Name.LocalName) + MakeSelectionPath(sourceNode, trackingItem.Keys);

                var itemName = trackingItem.Name.Replace("*", sourceNode.Parent.Name.LocalName) +  MakeSelectionName(sourceNode, trackingItem.Keys);

                var targetNode = target.XPathSelectElement(itemPath);

                if (targetNode == null)
                {
                    // missing from target - its a delete
                    changes.Add(uSyncChange.Delete(trackingItem.Path, itemName, sourceNode.Name.LocalName));
                }
                else
                {
                    // check the node to see if its an update. 
                    changes.AddRange(CompareNode(targetNode, sourceNode, trackingItem.Path, itemName, trackingItem.MaskValue));
                }
            }

            return changes;
        }

        private string MakeSelectionPath(XElement node, string keys)
        {
            if (keys == "#") return node.Name.LocalName;
            var selectionPath = "";

            var keyList = keys.ToDelimitedList();

            foreach (var key in keyList)
            {
                var value = GetKeyValue(node, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    selectionPath += $"[{key} = '{value}']";
                }

            }

            return selectionPath.Replace("][", " and ");
        }

        private string MakeSelectionName(XElement node, string keys)
        {
            var name = "";
            var names = new List<string>();
            var keyList = keys.ToDelimitedList();
            foreach (var key in keyList)
            {
                var value = GetKeyValue(node, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    names.Add(value);
                }
            }

            if (names.Count > 0) return $" ({string.Join(" ", names)})";
            return "";
        }

        private string GetKeyValue(XElement node, string key)
        {
            if (key == "#") return node.Name.LocalName;
            if (key.StartsWith("@")) return node.Attribute(key.Substring(1)).ValueOrDefault(string.Empty);
            return node.Element(key).ValueOrDefault(string.Empty);
        }

        private IEnumerable<uSyncChange> CompareNode(XElement target, XElement source, string path, string name, bool maskValue)
        {
            var changes = new List<uSyncChange>();

            // compare attributes
            foreach (var sourceAttribute in source.Attributes())
            {
                var sourceAttributeName = sourceAttribute.Name.LocalName;

                changes.AddNotNull(
                    Compare(
                        sourceAttribute.Value,
                        target.Attribute(sourceAttributeName).ValueOrDefault(string.Empty),
                        path + $"{seperator}{sourceAttributeName}",
                        $"{name} > {sourceAttributeName}", maskValue));


            }

            if (source.HasElements)
            {
                // compare elements. 
                foreach (var sourceElement in source.Elements())
                {

                    var sourceElementName = sourceElement.Name.LocalName;

                    changes.AddNotNull(
                        Compare(
                            target.Element(sourceElementName).ValueOrDefault(string.Empty),
                            sourceElement.Value,
                            path + $"{seperator}{sourceElementName}",
                            $"{name} > {sourceElementName}", maskValue));

                }
            }
            else
            {
                changes.AddNotNull(Compare(target.ValueOrDefault(string.Empty), source.ValueOrDefault(string.Empty), path, name, maskValue));
            }

            return changes;
        }

        private uSyncChange Compare(string target, string source, string path, string name, bool maskValue)
        {
            if (source.DetectIsJson())
            {
                return JsonChange(target, source, path, name, maskValue);
            }
            else
            {
                return StringChange(target, source, path, name, maskValue);
            }
        }
        private uSyncChange JsonChange(string target, string source, string path, string name, bool maskValue)
        {
            try
            {
                var sourceJson = JsonConvert.DeserializeObject<JToken>(source);
                var targetJson = JsonConvert.DeserializeObject<JToken>(target);

                if (JToken.DeepEquals(sourceJson, targetJson)) return null;

                return uSyncChange.Update(path, name, sourceJson, targetJson);
            }
            catch
            {
                return StringChange(target, source, path, name, maskValue);
            }
        }
        private uSyncChange StringChange(string target, string source, string path, string name, bool maskValue)
        {
            if (source.Equals(target)) return null;
            return uSyncChange.Update(path, name, maskValue ? "*****" : source, maskValue ? "*****" : target);
        }


        public virtual List<TrackingItem> TrackingItems { get; }

    }

    public class TrackingItem
    {
        public static TrackingItem Single(string name, string path)
            => new TrackingItem(name, path, true);

        public static TrackingItem Attribute(string name, string path, string attributes)
            => new TrackingItem(name, path, true) { AttributeKey = attributes };

        public static TrackingItem Many(string name, string path, string keys)
            => new TrackingItem(name, path, false)
            {
                Keys = keys
            };

        public TrackingItem(string name, string path, bool single)
        {
            this.SingleItem = single;
            this.Name = name;
            this.Path = path;
        }

        public bool SingleItem { get; set; }
        public string Path { get; set; }

        public string Name { get; set; }

        public string AttributeKey { get; set; }

        public string Keys { get; set; }

        public bool MaskValue { get; set; }

        public bool HasKeys() => !string.IsNullOrWhiteSpace(Keys);
        public bool HasAttributes() => !string.IsNullOrWhiteSpace(AttributeKey);
    }

    public class TrackingKey
    {
        public string Key { get; set; }
        public bool IsAttribute { get; set; }
    }
}
