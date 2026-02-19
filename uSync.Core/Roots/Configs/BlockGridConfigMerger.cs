using System.Text.Json.Nodes;

using Umbraco.Cms.Core;

using uSync.Core.Extensions;
using uSync.Core.Roots.Models;

namespace uSync.Core.Roots.Configs;

/// <summary>
///  merges the blocks and block groups in a BlockGrid datatype.
/// </summary>
/// <remarks>
///  because the backend doesn't know all the properties in a block grid anymore
///  we have to merge this based on the json values.
/// </remarks>
internal class BlockGridConfigMerger : BlockListMergerBase, ISyncConfigMerger
{
    public string[] Editors => [
        Constants.PropertyEditors.Aliases.BlockGrid
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
