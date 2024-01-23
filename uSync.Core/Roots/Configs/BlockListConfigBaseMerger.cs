using System;
using System.Linq;

using Umbraco.Extensions;

namespace uSync.Core.Roots.Configs;

internal class BlockListConfigBaseMerger 
{
    protected static string _removedLabel = "uSync:Removed in child site.";

    protected static TObject[] MergeObjects<TObject, TKey>(TObject[] rootObject, TObject[] targetObject, Func<TObject, TKey> keySelector, Predicate<TObject> predicate)
    {
        var targetBlockKeys = targetObject.Select(keySelector);
        var blocksFromRoot = rootObject.Where(x => !targetBlockKeys.Contains(keySelector(x)));

        if (blocksFromRoot.Any())
        {
            var mergedBlocks = targetObject
                .Concat(blocksFromRoot)
                .ToList();

            mergedBlocks.RemoveAll(predicate);

            return mergedBlocks.ToArray();
        }

        return targetObject;
    }

    protected TObject[] GetObjectDifferences<TObject, TKey>(TObject[] rootBlocks, TObject[] targetBlocks, Func<TObject, TKey> keySelector, Action<TObject, string> setMarker)
    {
        var rootBlockKeys = rootBlocks.Select(keySelector);
        var targetBlockKeys = targetBlocks.Select(keySelector);

        var remaining =
            targetBlocks.Where(x => !rootBlockKeys.Contains(keySelector(x)))
            .ToList();

        var removedKeys = rootBlockKeys.Except(targetBlockKeys);

        foreach (var removedBlock in rootBlocks.Where(x => removedKeys.Contains(keySelector(x))))
        {
            setMarker(removedBlock, _removedLabel);
            remaining.Add(removedBlock);
        }

        return remaining.ToArray();
    }

}
