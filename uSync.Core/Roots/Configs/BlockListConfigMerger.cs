//using Umbraco.Cms.Core;
//using Umbraco.Cms.Core.PropertyEditors;

//using uSync.Core.Extensions;

//namespace uSync.Core.Roots.Configs;

///// <summary>
/////  merges blocklist configs. 
///// </summary>
//internal class BlockListConfigMerger : SyncConfigMergerBase, ISyncConfigMerger
//{
//    public virtual string[] Editors => [
//        Constants.PropertyEditors.Aliases.BlockList
//    ];


//    public virtual object GetMergedConfig(string root, string target)
//    {
//        var rootConfig = root.DeserializeJson<BlockListConfiguration>();
//        var targetConfig = target.DeserializeJson<BlockListConfiguration>();

//        targetConfig.Blocks = MergeObjects(
//            rootConfig.Blocks,
//            targetConfig.Blocks,
//            x => x.ContentElementTypeKey,
//            x => x.Label?.StartsWith(_removedLabel) == true);

//        return targetConfig;
//    }

//    public virtual object GetDifferenceConfig(string root, string target)
//    {
//        var rootConfig = root.DeserializeJson<BlockListConfiguration>();
//        var targetConfig = target.DeserializeJson<BlockListConfiguration>();


//        targetConfig.Blocks = GetObjectDifferences(
//            rootConfig.Blocks,
//            targetConfig.Blocks,
//            x => x.ContentElementTypeKey,
//            (x, label) => x.Label = $"{_removedLabel}:{x.Label}");

//        return targetConfig;
//    }

//}