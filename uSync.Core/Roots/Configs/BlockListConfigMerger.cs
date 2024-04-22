using Newtonsoft.Json;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.PropertyEditors;

namespace uSync.Core.Roots.Configs;

/// <summary>
///  merges blocklist configs. 
/// </summary>
internal class BlockListConfigMerger : SyncConfigMergerBase, ISyncConfigMerger
{
    public virtual string[] Editors => [
        Constants.PropertyEditors.Aliases.BlockList
    ];


    public virtual object GetMergedConfig(string root, string target)
    {
        var rootConfig = JsonConvert.DeserializeObject<BlockListConfiguration>(root);
        var targetConfig = JsonConvert.DeserializeObject<BlockListConfiguration>(target);

        targetConfig.Blocks = MergeObjects(
            rootConfig.Blocks,
            targetConfig.Blocks,
            x => x.ContentElementTypeKey,
            x => x.Label?.StartsWith(_removedLabel) == true);

        return targetConfig;
    }

    public virtual object GetDifferenceConfig(string root, string target)
    {
        var rootConfig = JsonConvert.DeserializeObject<BlockListConfiguration>(root);
        var targetConfig = JsonConvert.DeserializeObject<BlockListConfiguration>(target);


        targetConfig.Blocks = GetObjectDifferences(
            rootConfig.Blocks, 
            targetConfig.Blocks,
            x => x.ContentElementTypeKey,
            (x, label) => x.Label = $"{_removedLabel}:{x.Label}");

        if (targetConfig.Blocks.Length == 0) return null;

        return targetConfig;
    }
   
}