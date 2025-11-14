using Json.More;

using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Extensions;

using uSync.Core.Extensions;
using uSync.Core.Roots.Models;

namespace uSync.Core.Roots.Configs;

internal abstract class SyncConfigMergerBase
{
    protected static string _removedLabel = "uSync:Removed in child site.";
    protected static string _inheritedValue = "uSync:Inherited from root.";

    public abstract Dictionary<string, (string key, string label)> _knownArrayKeys { get; }

    protected TConfig? TryGetConfiguration<TConfig>(string value)
    {
        try
        {
            return value.DeserializeJson<TConfig>();
        }
        catch
        {
            return default;
        }
    }

    protected static TObject[] MergeObjects<TObject, TKey>(TObject[] rootObject, TObject[]? targetObject, Func<TObject, TKey> keySelector, Predicate<TObject> predicate)
    {
        var targetObjectKeys = targetObject?.Select(keySelector) ?? [];

        if (targetObjectKeys is IEnumerable<string> targetStrings)
        {
            targetObjectKeys = (IEnumerable<TKey>)targetStrings.Select(x => x.Replace($"{_removedLabel}:", ""));
        }

        var validRootObjects = rootObject?.Where(x => !targetObjectKeys.Contains(keySelector(x))).ToList()
            ?? [];

        var mergedObject = targetObject?.ToList() ?? [];

        if (validRootObjects.Count > 0)
        {
            mergedObject.AddRange(validRootObjects);
        }

        var x = mergedObject.RemoveAll(predicate);
        return [.. mergedObject];
    }

    protected static TObject[] GetObjectDifferences<TObject, TKey>(TObject[]? rootObject, TObject[]? targetObject, Func<TObject, TKey> keySelector, Action<TObject, string> setMarker)
    {
        var rootObjectKeys = rootObject?.Select(keySelector) ?? [];
        var targetObjectKeys = targetObject?.Select(keySelector) ?? [];

        var remaining =
            targetObject?.Where(x => !rootObjectKeys.Contains(keySelector(x)))
            .ToList() ?? [];

        var removedKeys = rootObjectKeys.Except(targetObjectKeys);
        var removals = rootObject?.Where(x => removedKeys.Contains(keySelector(x))) ?? [];

        foreach (var removedObject in removals)
        {
            setMarker(removedObject, _removedLabel);
            remaining.Add(removedObject);
        }

        return [.. remaining];
    }

    protected JsonArray? GetJsonArrayDifferences(JsonArray? sourceArray, JsonArray? targetArray, string key, string removeProperty, SyncFileMergeOptions options)
    {
        // if target is blank the difference is nothing?
        if (targetArray is null) return [];

        var sourceItems = sourceArray?
            .Select(x => x as JsonObject)?
            .WhereNotNull()
            .ToDictionary(k => k.TryGetPropertyAsObject(key, out var sourceKey) ? sourceKey.GetValueAsString(key) ?? sourceKey.ToString() : "", v => v) ?? [];

        var targetItems = targetArray?
            .Select(x => x as JsonObject)?
            .WhereNotNull()
            .ToDictionary(k => k.TryGetPropertyAsObject(key, out var targetKey) ? targetKey.GetValueAsString(key) ?? targetKey.ToString() : "", v => v) ?? [];

        // things that are only in the target. 
        var targetOnly = targetItems.Where(x => sourceItems.ContainsKey(x.Key) is false).Select(x => x.Value).ToList() ?? [];

        foreach (var block in targetItems)
        {
            var sourceItem = sourceItems.GetValueOrDefault(block.Key);
            if (sourceItem is null) continue;

            if (block.Value.IsJsonEqual(sourceItem) is false)
            {
                // the values are different, so we go property by property to see if we can merge them.
                var target = GetJsonPropertyDifferences(sourceItem, block.Value, key, options);
                targetOnly.Add(target);
            }
        }

        // keys that are only in the source have been removed from the child, we need to mark them as removed. 
        foreach (var removedItem in sourceItems.Where(x => targetItems.ContainsKey(x.Key) is false))
        {
            removedItem.Value[removeProperty] = _removedLabel;
            targetOnly.Add(removedItem.Value);
        }

        return targetOnly.ToJsonArray();
    }

    public JsonObject GetJsonPropertyDifferences(JsonObject sourceObject, JsonObject targetObject, string propertyKey, SyncFileMergeOptions options)
    {
        foreach (var property in sourceObject)
        {
            // if this is the key property or it's value is null skip it.
            if (property.Key == propertyKey || property.Value is null) continue;

            if (targetObject.TryGetPropertyValue(property.Key, out var targetValue) is false || targetValue is null)
            {
                // value isn't in target. inherit.
                // targetObject[property.Key] = JsonValue.Create(_inheritedValue);
                // inherit is implicit as missing properties are added on the merge.
                continue;
            }
            
            if (property.Value.IsJsonEqual(targetValue) is false)
            {
                // we don't merge past the top level unless we are magic merging. 
                if (options.MergeStrategy <= SyncMergeStrategy.Magic) continue;

                // target is an update so we keep this value.
                // unless its an array, and then we have to merge deeper. 
                switch (targetValue.GetValueKind())
                {
                    case JsonValueKind.Array:
                        var sourceArray = property.Value as JsonArray;
                        var targetArray = targetValue as JsonArray;

                        // this assumes we know what they key should be based on our array of well known array keys.
                        // if the array is new or generic we fall back to 'key';
                        var (arrayKey, arrayLabel) = _knownArrayKeys.GetValueOrDefault(property.Key, (key: "key", label: "label"));
                        targetObject[property.Key] = GetJsonArrayDifferences(sourceArray, targetArray, arrayKey, arrayLabel, options);
                        break;
                    case JsonValueKind.Object:
                        // i am not sure we ever hit this in the block config, but it's here should the block or json store
                        // an object in it. - the key is redundant, at this point, because a single object (not an array)
                        // wouldn't have a key. 
                        if (property.Value is JsonObject sourceObj && targetValue is JsonObject targetObj)
                        {
                            targetObject[property.Key] = GetJsonPropertyDifferences(sourceObj, targetObj, string.Empty, options);
                        }
                        break;
                }
            }
            else
            {
                // source wins.
                // targetObject[property.Key] = JsonValue.Create(_inheritedValue);

                // inherited is implicit.
                if (targetObject.ContainsKey(property.Key)) 
                    targetObject.Remove(property.Key);
            }
        }

        return targetObject;
    }

    protected JsonArray? MergeJsonArrays(JsonArray? sourceArray, JsonArray? targetArray, string key, string removeProperty)
    {
        // no source, we return target
        if (sourceArray is null) return targetArray;

        // no target we return source (we have to clone it).
        if (targetArray is null) return sourceArray.DeepClone() as JsonArray;

        // merge them. 
        foreach (var sourceItem in sourceArray)
        {
            if (sourceItem is not JsonObject sourceObject) continue;
            if (sourceObject.TryGetPropertyAsObject(key, out var sourceKey) is false) continue;

            var targetObject = targetArray
                .Select(x => x as JsonObject)
                .WhereNotNull()
                .FirstOrDefault(x => x?.TryGetPropertyAsObject(key, out var targetKey) == true && targetKey.GetValueAsString(key) == sourceKey.GetValueAsString(key));

            if (targetObject is null)
            {
                var clonedItem = sourceObject.SerializeJsonString().DeserializeJson<JsonObject>();
                targetArray.Add(clonedItem);
            }
            else
            {
                if (sourceItem.IsJsonEqual(targetObject) is false)
                {
                    // they are different we need to merge the properties. 
                    targetObject = MergeJsonProperties(sourceObject, targetObject, key);
                }
            }
        }

        List<int> removals = [];
        for (int i = 0; i < targetArray.Count; i++)
        {
            if (targetArray[i] is not JsonObject targetObject) continue;
            if (targetObject.ContainsKey(removeProperty) is false) continue;

            if (targetObject[removeProperty]!.ToString().StartsWith(_removedLabel) is true)
            {
                // we can't remove it while iterating, so add to a list. 
                removals.Add(i);
                continue;
            }

            // if the item has been removed from source, but the target has
            // values inherited from source we need to now remove it?
            var targetJson = targetObject.SerializeJsonString(false);
            if (targetJson.Contains(_inheritedValue) is true)
            {
                removals.Add(i);
            }
        }

        foreach (var index in removals.OrderDescending())
        {
            targetArray.RemoveAt(index);
        }

        return targetArray;
    }

    public JsonObject MergeJsonProperties(JsonObject sourceObject, JsonObject targetObject, string propertyKey)
    {
        var targetItems = targetObject.ToDictionary(
            k => k.Key, v => v.Value);

        foreach (var property in targetItems)
        {
            if (property.Key == propertyKey || property.Value is null) continue;

            switch (property.Value.GetValueKind())
            {
                case JsonValueKind.Array:
                    var sourceArray = sourceObject.GetPropertyAsArray(property.Key);
                    var targetArray = targetObject.GetPropertyAsArray(property.Key);
                    var (arrayKey, arrayLabel) = _knownArrayKeys.GetValueOrDefault(property.Key, (key: "key", label: "label"));
                    targetObject[property.Key] = MergeJsonArrays(sourceArray, targetArray, arrayKey, arrayLabel);
                    continue;
                case JsonValueKind.Object:
                    var sourcePropertyObject = sourceObject.GetPropertyAsObject(property.Key);
                    if (sourcePropertyObject is not null && property.Value is JsonObject targetPropertyObject)
                    {
                        targetObject[property.Key] = MergeJsonProperties(sourcePropertyObject, targetPropertyObject, string.Empty);
                    }
                    continue;
            }

            if (property.Value.ToString() == _inheritedValue)
            {
                if (sourceObject.TryGetPropertyValue(property.Key, out var sourceValue) is false)
                {
                    // is this an error?
                    // It means the value doesn't exist in the source, but at some point it has,
                    // because we have inherited it from the source. If it's a case of the values
                    // that are not set just not making it to the JSON then we can remove the
                    // property from target, and that is the same as it inheriting it from the source. 
                    targetObject.Remove(property.Key);
                    continue;
                }

                targetObject[property.Key] = sourceValue?.DeepClone();
            }
        }

        // properties that are set on the base, but not on the target need to be copied over. 
        var sourceOnly = sourceObject.ToDictionary(k => k.Key, v => v.Value)
            .Where(k => targetItems.ContainsKey(k.Key) is false);

        foreach(var sourceProperty in sourceOnly)
        {
            targetObject[sourceProperty.Key] = sourceProperty.Value?.DeepClone();
        }

        return targetObject;
    }
}
