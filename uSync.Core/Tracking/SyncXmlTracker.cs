using System.Text.Json.Nodes;
using System.Xml.Linq;
using System.Xml.XPath;

using Umbraco.Extensions;

using uSync.Core.Extensions;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking;

/// <summary>
///  tracks the differences in the xml between two items. 
/// </summary>
public class SyncXmlTracker<TObject>
{
    protected ISyncSerializer<TObject>? serializer;

    private const string _separator = "/";
    public IList<TrackingItem> Items { get; set; } = [];
    public virtual List<TrackingItem> TrackingItems { get; protected set; } = [];

    public SyncXmlTracker(SyncSerializerCollection serializers)
    {
        serializer = serializers.GetSerializer<TObject>();
    }

    protected virtual ISyncSerializer<TObject>? GetSerializer(XElement target)
        => serializer;

    protected virtual ISyncSerializer<TObject>? GetSerializer(TObject item)
        => serializer;


    public IEnumerable<uSyncChange> GetChanges(XElement target)
        => GetChanges(target, new SyncSerializerOptions());

    public IEnumerable<uSyncChange> GetChanges(XElement target, SyncSerializerOptions options) 
        => GetChangesAsync(target, options).Result;

    public async Task<IEnumerable<uSyncChange>> GetChangesAsync(XElement target, SyncSerializerOptions options)
    {
        var s = GetSerializer(target);
        if (s is not null)
        {
            var item = await s.FindItemAsync(target);
            if (item is not null)
            {
                var attempt = await SerializeItemAsync(item, options);
                if (attempt.Success is true && attempt.Item is not null)
                    return await GetChangesAsync(target, attempt.Item, options);
            }
        }

        return await GetChangesAsync(target, XElement.Parse("<blank/>"), options);
    }

    private async Task<SyncAttempt<XElement>> SerializeItemAsync(TObject item, SyncSerializerOptions options)
    {
        var serializer = GetSerializer(item);
        if (serializer is null)
            return SyncAttempt<XElement>.Fail("Unknown", ChangeType.Fail, "Failed to serialize");

        return await serializer.SerializeAsync(item, options);
    }

    [Obsolete("Use GetChangesAsync")]
    public IEnumerable<uSyncChange> GetChanges(XElement target, XElement source, SyncSerializerOptions options)
        => GetChangesAsync(target, source, options).Result;

    public async Task<IEnumerable<uSyncChange>> GetChangesAsync(XElement target, XElement source, SyncSerializerOptions options)
    {
        if (TrackingItems == null)
            return [];

        if (target.IsEmptyItem())
            return SyncXmlTracker<TObject>.GetEmptyFileChange(target, source).AsEnumerableOfOne();

        if (GetSerializer(target)?.IsValid(target) is false)
            return uSyncChange.Error("", "Invalid File", target.Name.LocalName).AsEnumerableOfOne();

        var changeType = await GetChangeTypeAsync(target, source, options);
        if (changeType == ChangeType.NoChange)
            return uSyncChange.NoChange("", target.GetAlias()).AsEnumerableOfOne();

        return CalculateDifferences(target, source);
    }

    private static uSyncChange GetEmptyFileChange(XElement target, XElement source)
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

    private async Task<ChangeType> GetChangeTypeAsync(XElement target, XElement source, SyncSerializerOptions options)
    {
        var s = GetSerializer(target);
        if (s is null) return ChangeType.Fail;

        return await s.IsCurrentAsync(target, source, options);
    }

    /// <summary>
    ///  actually kicks off here, if you have two xml files that are different. 
    /// </summary>

    private IEnumerable<uSyncChange> CalculateDifferences(XElement target, XElement source)
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

    private uSyncChange? TrackSingleItem(TrackingItem item, XElement target, XElement source, TrackingDirection direction)
    {
        var sourceNode = source.XPathSelectElement(item.Path);
        var targetNode = target.XPathSelectElement(item.Path);

        if (sourceNode != null)
        {
            if (targetNode == null)
            {
                // value is missing, this is a delete or create depending on compare direction
                return SyncXmlTracker<TObject>.AddMissingChange(item.Path, item.Name, sourceNode.ValueOrDefault(string.Empty), direction);
            }

            // only track updates when tracking target to source. 
            else if (direction == TrackingDirection.TargetToSource)
            {

                if (item.HasAttributes() && item.AttributeKey is not null)
                {
                    return SyncXmlTracker<TObject>.Compare(targetNode.Attribute(item.AttributeKey).ValueOrDefault(string.Empty),
                        sourceNode.Attribute(item.AttributeKey).ValueOrDefault(string.Empty),
                        item.Path, item.Name, item.MaskValue);
                }
                else
                {
                    return SyncXmlTracker<TObject>.Compare(targetNode.ValueOrDefault(string.Empty),
                        sourceNode.ValueOrDefault(string.Empty),
                        item.Path, item.Name, item.MaskValue);
                }
            }
        }

        return null;
    }

    private List<uSyncChange> TrackMultipleKeyedItems(TrackingItem trackingItem, XElement target, XElement source, TrackingDirection direction)
    {
        var changes = new List<uSyncChange>();

        var sourceItems = source.XPathSelectElements(trackingItem.Path);

        foreach (var sourceNode in sourceItems)
        {
            // make the selection path for this item.
            var itemPath = trackingItem.Path.Replace("*", sourceNode.Parent?.Name.LocalName) + MakeSelectionPath(sourceNode, trackingItem.Keys);

            var itemName = trackingItem.Name.Replace("*", sourceNode.Parent?.Name.LocalName) +
                MakeSelectionName(sourceNode, string.IsNullOrWhiteSpace(trackingItem.ValueKey) ? trackingItem.Keys : trackingItem.ValueKey);

            var targetNode = target.XPathSelectElement(itemPath);

            if (targetNode == null)
            {
                var value = sourceNode.ValueOrDefault(string.Empty);
                if (!string.IsNullOrEmpty(trackingItem.ValueKey))
                    value = SyncXmlTracker<TObject>.GetKeyValue(sourceNode, trackingItem.ValueKey);

                // missing, we add either a delete or create - depending on tracking direction
                changes.AddNotNull(SyncXmlTracker<TObject>.AddMissingChange(trackingItem.Path, itemName, value, direction));
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

    private string MakeSelectionPath(XElement node, string? keys)
    {
        if (keys is null) return node.Name.LocalName;
        if (keys == "#") return node.Name.LocalName;

        var selectionPath = "";

        var keyList = keys.ToDelimitedList();

        foreach (var key in keyList)
        {
            var value = SyncXmlTracker<TObject>.GetKeyValue(node, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                selectionPath += $"[{key} = {SyncXmlTracker<TObject>.EscapeXPathString(value)}]";
            }

        }

        return selectionPath.Replace("][", " and ");
    }

    private static string EscapeXPathString(string value)
    {
        if (!value.Contains('\''))
            return '\'' + value + '\'';

        if (!value.Contains('\"'))
            return '"' + value + '"';

        return "concat('" + value.Replace("'", "',\"'\",'") + "')";
    }


    private string MakeSelectionName(XElement node, string? keys)
    {
        if (keys is null) return string.Empty;

        var names = new List<string>();
        var keyList = keys.ToDelimitedList();
        foreach (var key in keyList)
        {
            var value = SyncXmlTracker<TObject>.GetKeyValue(node, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                names.Add(value);
            }
        }

        if (names.Count > 0) return $" ({string.Join(" ", names)})";
        return string.Empty;
    }

    private static string GetKeyValue(XElement node, string key)
    {
        if (key == "#") return node.Name.LocalName;
        if (key.StartsWith('@')) return node.Attribute(key.Substring(1)).ValueOrDefault(string.Empty);
        return node.Element(key).ValueOrDefault(string.Empty);
    }

    private List<uSyncChange> CompareNode(XElement target, XElement source, string path, string name, bool maskValue)
    {
        var changes = new List<uSyncChange>();

        // compare attributes
        foreach (var sourceAttribute in source.Attributes())
        {
            var sourceAttributeName = sourceAttribute.Name.LocalName;

            changes.AddNotNull(
                SyncXmlTracker<TObject>.Compare(
                    target.Attribute(sourceAttributeName).ValueOrDefault(string.Empty),
                    sourceAttribute.Value,
                    path + $"{_separator}{sourceAttributeName}",
                    $"{name} > {sourceAttributeName}", maskValue));
        }

        if (source.HasElements)
        {
            // compare elements. 
            foreach (var sourceElement in source.Elements())
            {

                var sourceElementName = sourceElement.Name.LocalName;

                changes.AddNotNull(
                    SyncXmlTracker<TObject>.Compare(
                        target.Element(sourceElementName).ValueOrDefault(string.Empty),
                        sourceElement.Value,
                        path + $"{_separator}{sourceElementName}",
                        $"{name} > {sourceElementName}", maskValue));

            }
        }
        else
        {
            changes.AddNotNull(SyncXmlTracker<TObject>.Compare(target.ValueOrDefault(string.Empty), source.ValueOrDefault(string.Empty), path, name, maskValue));
        }

        return changes;
    }

    private static uSyncChange? Compare(string target, string source, string path, string name, bool maskValue)
    {
        if (source.TryParseToJsonNode(out _) is true)
        {
            return SyncXmlTracker<TObject>.JsonChange(target, source, path, name, maskValue);
        }
        else
        {
            return SyncXmlTracker<TObject>.StringChange(target, source, path, name, maskValue);
        }
    }
    private static uSyncChange? JsonChange(string target, string source, string path, string name, bool maskValue)
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
            return SyncXmlTracker<TObject>.StringChange(target, source, path, name, maskValue);
        }
    }
    private static uSyncChange? StringChange(string target, string source, string path, string name, bool maskValue)
    {
        if (source.Equals(target)) return null;
        return uSyncChange.Update(path, name, maskValue ? "*****" : source, maskValue ? "*****" : target);
    }

    /// <summary>
    ///  Adds a change when a value is missing
    /// </summary>
    /// <remarks>
    ///  Depending on the direction of the compare this will add a delete (when value is missing from target)
    ///  or a create (when value is missing from source).
    /// </remarks>
    private static uSyncChange? AddMissingChange(string path, string name, string value, TrackingDirection direction)
    {
        return direction switch
        {
            TrackingDirection.TargetToSource => uSyncChange.Delete(path, name, value),
            TrackingDirection.SourceToTarget => uSyncChange.Create(path, name, value),
            _ => null,
        };
    }

    public virtual XElement? MergeFiles(XElement a, XElement b) => b;

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

    public bool SingleItem { get; set; }
    public string Path { get; set; }
    public string Name { get; set; }

    public string? SortingKey { get; set; }
    public string? ValueKey { get; set; }

    public string? AttributeKey { get; set; }
    public string? Keys { get; set; }

    public bool MaskValue { get; set; }

    public bool HasAttributes() => !string.IsNullOrWhiteSpace(AttributeKey);
}

public class TrackingKey
{
    public string? Key { get; set; }
    public bool IsAttribute { get; set; }
}

public enum TrackingDirection
{
    TargetToSource,
    SourceToTarget
}
