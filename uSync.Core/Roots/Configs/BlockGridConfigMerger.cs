using Newtonsoft.Json;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.PropertyEditors;

namespace uSync.Core.Roots.Configs;

internal class BlockGridConfigMerger : SyncConfigMergerBase, ISyncConfigMerger
{
    public string[] Editors => [
        Constants.PropertyEditors.Aliases.BlockGrid
    ];

    public virtual object GetMergedConfig(string root, string target)
    {
        var rootConfig = JsonConvert.DeserializeObject<BlockGridConfiguration>(root);
        var targetConfig = JsonConvert.DeserializeObject<BlockGridConfiguration>(target);

        targetConfig.Blocks = MergeObjects(
            rootConfig.Blocks,
            targetConfig.Blocks,
            x => x.ContentElementTypeKey,
            x => x.Label == _removedLabel);

        targetConfig.BlockGroups = MergeObjects
            (
            rootConfig.BlockGroups,
            targetConfig.BlockGroups,
            x => x.Name,
            x => x.Name?.StartsWith(_removedLabel) == true);

        if (targetConfig.BlockGroups.Length == 0 && targetConfig.Blocks.Length == 0)
            return null;

        return targetConfig;
    }

    public virtual object GetDifferenceConfig(string root, string target)
    {
        var rootConfig = JsonConvert.DeserializeObject<BlockGridConfiguration>(root);
        var targetConfig = JsonConvert.DeserializeObject<BlockGridConfiguration>(target);

        targetConfig.Blocks = GetObjectDifferences(
            rootConfig.Blocks,
            targetConfig.Blocks,
            x => x.ContentElementTypeKey,
            (x, label) => x.Label = _removedLabel);


        targetConfig.BlockGroups = GetObjectDifferences(
            rootConfig.BlockGroups,
            targetConfig.BlockGroups,
            x => x.Name,
            (x, name) => x.Name = $"{_removedLabel}:{x.Name}");
        
        return targetConfig;
    }  
}
