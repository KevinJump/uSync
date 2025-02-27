using System.Text.Json.Nodes;

using uSync.Core.Extensions;

namespace uSync.Core.Roots.Configs;

internal abstract class BlockListMergerBase : SyncConfigMergerBase
{
    protected JsonArray? GetMergedBlocks(JsonObject rootConfig, JsonObject targetConfig)
    {
        rootConfig.TryGetPropertyAsArray("blocks", out var rootBlocks);
        targetConfig.TryGetPropertyAsArray("blocks", out var targetBlocks);

        var merged = MergeJsonArrays(rootBlocks, targetBlocks,
            "contentElementTypeKey", "label");

        return merged?.Count > 0 ? merged : null;
    }

    protected JsonArray? GetBlockDifferences(JsonObject rootConfig, JsonObject targetConfig)
    {
        rootConfig.TryGetPropertyAsArray("blocks", out var rootBlocks);
        targetConfig.TryGetPropertyAsArray("blocks", out var targetBlocks);

        var diffrences = GetJsonArrayDifferences(rootBlocks, targetBlocks,
                        "contentElementTypeKey", "label") ?? [];

        return diffrences?.Count > 0 ? diffrences : null;
    }
}
