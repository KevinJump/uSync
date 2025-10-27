using System.Text.Json.Nodes;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.PropertyEditors;

using uSync.Core.Extensions;

namespace uSync.Core.Roots.Configs;
internal class ImageCropperConfigMerger : SyncConfigMergerBase, ISyncConfigMerger
{
    public override Dictionary<string, (string key, string label)> _knownArrayKeys => new()
    {
        {  "crops", (key: "alias", label: "removed") }
    };

    public string[] Editors => [
        Constants.PropertyEditors.Aliases.ImageCropper
    ];

    public object? GetMergedConfig(string root, string target)
    {
        var rootConfig = root.DeserializeJson<JsonObject>();
        var targetConfig = target.DeserializeJson<JsonObject>();

        if (rootConfig is null) return target;
        if (targetConfig is null) return root;

        return MergeJsonProperties(rootConfig, targetConfig, "_");
    }

    public object? GetDifferenceConfig(string root, string target)
    {
        var rootConfig = root.DeserializeJson<JsonObject>();
        var targetConfig = target.DeserializeJson<JsonObject>();

        if (targetConfig is null) return target;
        if (rootConfig is null) return target;

        return GetJsonPropertyDifferences(rootConfig, targetConfig, "_");
    }
}
