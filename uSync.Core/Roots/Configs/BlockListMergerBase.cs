using System.Text.Json.Nodes;

using uSync.Core.Extensions;

namespace uSync.Core.Roots.Configs;

internal abstract class BlockListMergerBase : SyncConfigMergerBase
{
    public override Dictionary<string, (string key, string label)> _knownArrayKeys => new() {
        { "blocks", (key: "contentElementTypeKey", label: "label") },
        { "blockGroups", (key: "key", label: "name") },
        { "areas", (key: "key", label: "alias") },
        { "specifiedAllowance", (key: "elementTypeKey", label: "removed") }
    };
}
