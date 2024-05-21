using System.Text.Json.Nodes;

using uSync.Core.Extensions;

namespace uSync.Core.Roots.Configs;

internal abstract class BlockListMergerBase : SyncConfigMergerBase
{
	protected JsonArray GetMergedBlocks(JsonObject rootConfig, JsonObject targetConfig)
	{
		rootConfig.TryGetPropertyAsArray("blocks", out var rootBlocks);
		targetConfig.TryGetPropertyAsArray("blocks", out var targetBlocks);

		return MergeJsonArrays(rootBlocks, targetBlocks,
			"contentElementTypeKey", "label") ?? [];
	}

	protected JsonArray GetBlockDifferences(JsonObject rootConfig, JsonObject targetConfig)
	{
		rootConfig.TryGetPropertyAsArray("blocks", out var rootBlocks);
		targetConfig.TryGetPropertyAsArray("blocks", out var targetBlocks);

		return GetJsonArrayDifferences(rootBlocks, targetBlocks,
						"contentElementTypeKey", "label") ?? [];
	}
}
