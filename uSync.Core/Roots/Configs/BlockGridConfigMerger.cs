//using Umbraco.Cms.Core;
//using Umbraco.Cms.Core.PropertyEditors;

//using uSync.Core.Extensions;

//namespace uSync.Core.Roots.Configs;

//internal class BlockGridConfigMerger : SyncConfigMergerBase, ISyncConfigMerger
//{
//    public string[] Editors => [
//        Constants.PropertyEditors.Aliases.BlockGrid
//    ];

//    public virtual object GetMergedConfig(string root, string target)
//    {
//        var rootConfig = root.DeserializeJson<BlockGridConfiguration>();
//        var targetConfig = target.DeserializeJson<BlockGridConfiguration>();

//        targetConfig.Blocks = MergeObjects(
//            rootConfig.Blocks,
//            targetConfig.Blocks,
//            x => x.ContentElementTypeKey,
//            x => x.Label == _removedLabel);

//        //targetConfig.BlockGroups = MergeObjects
//        //    (
//        //    rootConfig.BlockGroups,
//        //    targetConfig.BlockGroups,
//        //    x => x.Name,
//        //    x => x.Name?.StartsWith(_removedLabel) == true);

//        return targetConfig;
//    }

//    public virtual object GetDifferenceConfig(string root, string target)
//    {
//        var rootConfig = root.DeserializeJson<BlockGridConfiguration>();
//        var targetConfig = target.DeserializeJson<BlockGridConfiguration>();

//        targetConfig.Blocks = GetObjectDifferences(
//            rootConfig.Blocks,
//            targetConfig.Blocks,
//            x => x.ContentElementTypeKey,
//            (x, label) => x.Label = _removedLabel);


//        //targetConfig.BlockGroups = GetObjectDifferences(
//        //    rootConfig.BlockGroups,
//        //    targetConfig.BlockGroups,
//        //    x => x.Name,
//        //    (x, name) => x.Name = $"{_removedLabel}:{x.Name}");

//        return targetConfig;
//    }
//}
