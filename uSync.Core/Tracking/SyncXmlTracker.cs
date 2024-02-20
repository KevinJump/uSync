using System.Text.Json.Nodes;
using System.Xml.Linq;
using System.Xml.XPath;

using Umbraco.Extensions;

using uSync.Core.Extensions;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking;

/// <summary>
///  tracks the diffrences in the xml between two items. 
/// </summary>
public class SyncXmlTracker<TObject>
{
    protected ISyncSerializer<TObject> serializer;

    private const string seperator = "/";

    public SyncXmlTracker(SyncSerializerCollection serializers)
    {
        serializer = serializers.GetSerializer<TObject>();
    }

    protected virtual ISyncSerializer<TObject> GetSerializer(XElement target)
        => serializer;

    protected virtual ISyncSerializer<TObject> GetSerializer(TObject item)
        => serializer;

    public IList<TrackingItem> Items { get; set; }

    public IEnumerable<uSyncChange> GetChanges(XElement target)
        => GetChanges(target, new SyncSerializerOptions());

    public IEnumerable<uSyncChange> GetChanges(XElement target, SyncSerializerOptions options)
    {
        var item = GetSerializer(target).FindItem(target);
        if (item != null)
        {
            var attempt = SerializeItem(item, options);
            if (attempt.Success)
                return GetChanges(target, attempt.Item, options);

        }

        return GetChanges(target, XElement.Parse("<blank/>"), options);
    }

    private SyncAttempt<XElement> SerializeItem(TObject item, SyncSerializerOptions options)
        => GetSerializer(item).Serialize(item, options);

    public IEnumerable<uSyncChange> GetChanges(XElement target, XElement source, SyncSerializerOptions options)
    {
        if (TrackingItems == null)
            return Enumerable.Empty<uSyncChange>();

        if (target.IsEmptyItem())
            return GetEmptyFileChange(target, source).AsEnumerableOfOne();

        if (!GetSerializer(target).IsValid(target))
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
       => GetSerializer(target).IsCurrent(target, source, options);

    /// <summary>
    ///  actually kicks off here, if you have two xml files that are different. 
    /// </summary>

    private IEnumerable<uSyncChange> CalculateDiffrences(XElement target, XElement source)
    {
        var changes = new List<uSyncChange>();

        foreach (var trackingItem in this.TrackingItems)
        {
            if (trackingItem.SingleItem)
            {
                changes.AddNotNull(TrackSingleItem(trackingItem, target, source, TrackingDirection.TargetToSource));
                changes.AddNotNull(TrackSingleItem(trackingItem, source, target, TrackingDirection.SourceToTarget));
            }
            else
            {
                changes.AddRange(TrackMultipleKeyedItems(trackingItem, target, source, TrackingDirection.TargetToSource));
                changes.AddRange(TrackMultipleKeyedItems(trackingItem, source, target, TrackingDirection.SourceToTarget));
            }
        }

        return changes;
    }

    private uSyncChange TrackSingleItem(TrackingItem item, XElement target, XElement source, TrackingDirection direction)
    {
        var sourceNode = source.XPathSelectElement(item.Path);
        var targetNode = target.XPathSelectElement(item.Path);

        if (sourceNode != null)
        {
            if (targetNode == null)
            {
                // value is missing, this is a delete or create depending on compare direction
                return AddMissingChange(item.Path, item.Name, sourceNode.ValueOrDefault(string.Empty), direction);
            }

            // only track updates when tracking target to source. 
            else if (direction == TrackingDirection.TargetToSource)
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

    private IEnumerable<uSyncChange> TrackMultipleKeyedItems(TrackingItem trackingItem, XElement target, XElement source, TrackingDirection direction)
    {
        var changes = new List<uSyncChange>();

        var sourceItems = source.XPathSelectElements(trackingItem.Path);

        foreach (var sourceNode in sourceItems)
        {
            // make the selection path for this item.
            var itemPath = trackingItem.Path.Replace("*", sourceNode.Parent.Name.LocalName) + MakeSelectionPath(sourceNode, trackingItem.Keys);

            var itemName = trackingItem.Name.Replace("*", sourceNode.Parent.Name.LocalName) +
                MakeSelectionName(sourceNode, String.IsNullOrWhiteSpace(trackingItem.ValueKey) ? trackingItem.Keys : trackingItem.ValueKey);

            var targetNode = target.XPathSelectElement(itemPath);

            if (targetNode == null)
            {
                var value = sourceNode.ValueOrDefault(string.Empty);
                if (!string.IsNullOrEmpty(trackingItem.ValueKey))
                    value = GetKeyValue(sourceNode, trackingItem.ValueKey);

                // missing, we add either a delete or create - depending on tracking direction
                changes.AddNotNull(AddMissingChange(trackingItem.Path, itemName, value, direction));
            }

            // only track updates when tracking target to source. 
            else if (direction == TrackingDirection.TargetToSource)
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
                selectionPath += $"[{key} = {EscapeXPathString(value)}]";
            }

        }

        return selectionPath.Replace("][", " and ");
    }

    private string EscapeXPathString(string value)
    {
        if (!value.Contains("'"))
            return '\'' + value + '\'';

        if (!value.Contains("\""))
            return '"' + value + '"';

        return "concat('" + value.Replace("'", "',\"'\",'") + "')";
    }


    private string MakeSelectionName(XElement node, string keys)
    {
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
                    target.Attribute(sourceAttributeName).ValueOrDefault(string.Empty),
                    sourceAttribute.Value,
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
        if (source.TryParseToJsonNode(out _) is true)
        {
            return JsonChange(target, source, path, name, maskValue);
        }
        else
        {
            return StringChange(target, source, path, name, maskValue);
        }
    }
    private uSyncChange? JsonChange(string target, string source, string path, string name, bool maskValue)
    {
        try
        {
            var sourceJson = source.ConvertToJsonNode();
            var targetJson = target.ConvertToJsonNode();

            if (JsonNode.DeepEquals(sourceJson, targetJson)) return null;
            return uSyncChange.Update(path, name, sourceJson, targetJson);
        }
        catch
        {
            return StringChange(target, source, path, name, maskValue);
        }
    }
    private uSyncChange? StringChange(string target, string source, string path, string name, bool maskValue)
    {
        if (source.Equals(target)) return null;
        return uSyncChange.Update(path, name, maskValue ? "*****" : source, maskValue ? "*****" : target);
    }

    /// <summary>
    ///  Adds a change when a value is missing
    /// </summary>
    /// <remarks>
    ///  Depending on the direction of the comapre this will add a delete (when value is mising from target)
    ///  or a create (when value is missing from source).
    /// </remarks>
    private uSyncChange? AddMissingChange(string path, string name, string value, TrackingDirection direction)
    {
        switch (direction)
        {
            case TrackingDirection.TargetToSource:
                return uSyncChange.Delete(path, name, value);
            case TrackingDirection.SourceToTarget:
                return uSyncChange.Create(path, name, value);
        }

        return null;
    }

    public virtual List<TrackingItem> TrackingItems { get; }

    public virtual XElement MergeFiles(XElement a, XElement b) => b;

    public virtual XElement? GetDifferences(List<XElement> nodes)
        => nodes?.Count > 0 ? nodes[^1] : null;

}

public class TrackingItem
{
    public static TrackingItem Single(string name, string path)
        => new TrackingItem(name, path, true);

    public static TrackingItem Attribute(string name, string path, string attributes)
        => new TrackingItem(name, path, true) { AttributeKey = attributes };

    public static TrackingItem Many(string name, string path, string keys)
        => new TrackingItem(name, path, false, keys);

    public static TrackingItem Many(string name, string path, string keys, string valueKey)
        => new TrackingItem(name, path, false, keys, valueKey);

    public static TrackingItem Many(string name, string path, string key, string valueKey, string sortedKey)
        => new TrackingItem(name, path, false, key, valueKey)
        {
            SortingKey = sortedKey
        };

    public TrackingItem(string name, string path, bool single)
    {
        this.SingleItem = single;
        this.Name = name;
        this.Path = path;
    }

    public TrackingItem(string name, string path, bool single, string keys)
        : this(name, path, single)
    {
        Keys = keys;
    }

    public TrackingItem(string name, string path, bool single, string keys, string valueKey)
        : this(name, path, single, keys)
    {
        this.ValueKey = valueKey;
    }

    // the key something is sorted by. 
    public string SortingKey { get; set; }

    public bool SingleItem { get; set; }
    public string Path { get; set; }
    public string Name { get; set; }

    public string ValueKey { get; set; }

    public string AttributeKey { get; set; }
    public string Keys { get; set; }

    public bool MaskValue { get; set; }

    public bool HasAttributes() => !string.IsNullOrWhiteSpace(AttributeKey);
}

public class TrackingKey
{
    public string Key { get; set; }
    public bool IsAttribute { get; set; }
}

public enum TrackingDirection
{
    TargetToSource,
    SourceToTarget
}
