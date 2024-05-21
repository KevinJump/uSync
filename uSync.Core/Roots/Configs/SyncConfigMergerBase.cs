using System.Text.Json.Nodes;

using Umbraco.Extensions;

using uSync.Core.Extensions;

namespace uSync.Core.Roots.Configs;

internal class SyncConfigMergerBase
{
    protected static string _removedLabel = "uSync:Removed in child site.";

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
        return mergedObject.ToArray();
    }

    protected TObject[] GetObjectDifferences<TObject, TKey>(TObject[]? rootObject, TObject[]? targetObject, Func<TObject, TKey> keySelector, Action<TObject, string> setMarker)
    {
        var rootObjectKeys = rootObject?.Select(keySelector) ?? Enumerable.Empty<TKey>();
        var targetObjectKeys = targetObject?.Select(keySelector) ?? [];

        var remaining =
            targetObject?.Where(x => !rootObjectKeys.Contains(keySelector(x)))
            .ToList() ?? [];

        var removedKeys = rootObjectKeys.Except(targetObjectKeys);
        var removals = rootObject?.Where(x => removedKeys.Contains(keySelector(x))) ?? Enumerable.Empty<TObject>();

        foreach (var removedObject in removals)
        {
            setMarker(removedObject, _removedLabel);
            remaining.Add(removedObject);
        }

        return [.. remaining];
    }

    protected static JsonArray? GetJsonArrayDifferences(JsonArray? sourceArray, JsonArray? targetArray, string key, string removeProperty)
    {
		// if target is blank the difference is nothing?
		if (targetArray is null) return [];

        var sourceItems = sourceArray?
            .Select(x => x as JsonObject)?
            .WhereNotNull()
            .ToDictionary(k => k.TryGetPropertyAsObject(key, out var sourceKey) ? sourceKey.ToString() : "", v => v) ?? [];

        var targetItems = targetArray?
            .Select(x => x as JsonObject)?
            .WhereNotNull()
            .ToDictionary(k => k.TryGetPropertyAsObject(key, out var targetKey) ? targetKey.ToString() : "", v => v) ?? [];

        // things that are only in the target. 
        var targetOnly = targetItems.Where(x => sourceItems.ContainsKey(x.Key) is false).Select(x => x.Value).ToList() ?? [];

		// keys that are only in the source have been removed from the child, we need to mark them as removed. 
		foreach(var removedItem in sourceItems.Where(x => targetItems.ContainsKey(x.Key) is false))
        {
            if (removedItem.Value.ContainsKey(removeProperty))
                removedItem.Value[removeProperty] = _removedLabel;

            targetOnly.Add(removedItem.Value);
        }

        return [.. targetOnly];
	}

    protected static JsonArray? MergeJsonArrays(JsonArray? sourceArray, JsonArray? targetArray, string key, string removeProperty)
    {
        // no source, we return target
        if (sourceArray is null) return targetArray;

        // no target we return source 
        if (targetArray is null) return sourceArray;

        // merge them. 
		foreach (var sourceItem in sourceArray)
        {
            var sourceObject = sourceItem as JsonObject;
            if (sourceObject is null) continue;
            if (sourceObject.TryGetPropertyAsObject(key, out var sourceKey) is false) continue;

            var targetObject = targetArray.FirstOrDefault(
                x => (x as JsonObject)?.TryGetPropertyAsObject(key, out var targetKey) == true && targetKey == sourceKey) as JsonObject;

            if (targetObject is null)
            {
                var clonedItem = sourceObject.SerializeJsonString().DeserializeJson<JsonObject>();
                targetArray.Add(clonedItem);
			}
        }

        // removals. 
        foreach(var targetItem in targetArray)
        {
            var targetObject = targetItem as JsonObject;
            if (targetObject is null) continue;

            if (targetObject.ContainsKey(removeProperty) is false) continue;

            if (targetObject[removeProperty]!.ToString().StartsWith(_removedLabel) is true)
            {
                // remove it. 
                targetArray.Remove(targetItem);
			}
		}

        return targetArray;
    }

}
