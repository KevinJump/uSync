using System.Text.Json.Nodes;

using Umbraco.Cms.Core;

using uSync.Core.Extensions;
using uSync.Core.Roots.Models;

namespace uSync.Core.Roots.Configs;

/// <summary>
///  merges blocklist configs. 
/// </summary>
/// <remarks>
///  the back-office no longer has an object to represent everything 
///  that might be stored in a block, so we have to merge based on the json
/// </remarks>
internal class BlockListConfigMerger : BlockListMergerBase, ISyncConfigMerger
{
    public virtual string[] Editors => [
        Constants.PropertyEditors.Aliases.BlockList
    ];

    public virtual object GetMergedConfig(string root, string target)
    {
        var rootConfig = root.DeserializeJson<JsonObject>();
        var targetConfig = target.DeserializeJson<JsonObject>();

        if (rootConfig is null) return target;
        if (targetConfig is null) return root;

        return MergeJsonProperties(rootConfig, targetConfig, "_");
    }

    public virtual object GetDifferenceConfig(string root, string target, SyncFileMergeOptions options)
    {
        var rootConfig = root.DeserializeJson<JsonObject>();
        var targetConfig = target.DeserializeJson<JsonObject>();

        if (targetConfig is null) return target;
        if (rootConfig is null) return target;

        return GetJsonPropertyDifferences(rootConfig, targetConfig, "_", options);
    }

}