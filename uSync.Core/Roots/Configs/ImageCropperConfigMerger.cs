using Umbraco.Cms.Core;
using Umbraco.Cms.Core.PropertyEditors;

namespace uSync.Core.Roots.Configs;
internal class ImageCropperConfigMerger : SyncConfigMergerBase, ISyncConfigMerger
{
    public string[] Editors => [
        Constants.PropertyEditors.Aliases.ImageCropper
    ];

    public object GetMergedConfig(string root, string target)
    {
        var rootConfig = TryGetConfiguration<ImageCropperConfiguration>(root);
        var targetConfig = TryGetConfiguration<ImageCropperConfiguration>(target);

        targetConfig.Crops = MergeObjects(
            rootConfig.Crops,
            targetConfig.Crops,
            x => x.Alias,
            x => x.Alias?.StartsWith(_removedLabel) == true);

        return targetConfig; 
    }

    public object GetDifferenceConfig(string root, string target)
    {
        var rootConfig = TryGetConfiguration<ImageCropperConfiguration>(root);
        var targetConfig = TryGetConfiguration<ImageCropperConfiguration>(target);

        targetConfig.Crops = GetObjectDifferences(
            rootConfig.Crops,
            targetConfig.Crops,
            x => x.Alias,
            (x, label) => x.Alias = $"{_removedLabel}:{x.Alias}");

        if (targetConfig.Crops.Length == 0) return null;

        return targetConfig;
    }
}
