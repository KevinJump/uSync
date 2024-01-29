using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Umbraco.Extensions;

namespace uSync.Core.Roots.Configs;

internal class SyncConfigMergerBase 
{
    protected static string _removedLabel = "uSync:Removed in child site.";

    protected TConfig TryGetConfiguration<TConfig>(string value)
    {
        try
        {
            return JsonConvert.DeserializeObject<TConfig>(value);
        }
        catch
        {
            return default;
        }
    }


    protected static TObject[] MergeObjects<TObject, TKey>(TObject[] rootObject, TObject[] targetObject, Func<TObject, TKey> keySelector, Predicate<TObject> predicate)
    {
        var targetObjectKeys = targetObject.Select(keySelector);

        if (targetObjectKeys is IEnumerable<string> targetStrings)
        {
            targetObjectKeys = (IEnumerable<TKey>)targetStrings.Select(x => x.Replace($"{_removedLabel}:", ""));
        }

        var validRootObjects = rootObject?.Where(x => !targetObjectKeys.Contains(keySelector(x))).ToList()
            ?? [];

        var mergedObject = targetObject.ToList();

        if (validRootObjects.Count > 0)
        {
            mergedObject.AddRange(validRootObjects);
        }

        var x = mergedObject.RemoveAll(predicate);
        return mergedObject.ToArray();
    }

    protected TObject[] GetObjectDifferences<TObject, TKey>(TObject[] rootObject, TObject[] targetObject, Func<TObject, TKey> keySelector, Action<TObject, string> setMarker)
    {
        var rootObjectKeys = rootObject?.Select(keySelector) ?? Enumerable.Empty<TKey>();
        var targetObjectKeys = targetObject.Select(keySelector);

        var remaining =
            targetObject.Where(x => !rootObjectKeys.Contains(keySelector(x)))
            .ToList();

        var removedKeys = rootObjectKeys.Except(targetObjectKeys);
        var removals = rootObject?.Where(x => removedKeys.Contains(keySelector(x))) ?? Enumerable.Empty<TObject>();

        foreach (var removedObject in removals )
        {
            setMarker(removedObject, _removedLabel);
            remaining.Add(removedObject);
        }

        return remaining.ToArray();
    }

}
