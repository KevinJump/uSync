using uSync.Core.Roots.Models;

namespace uSync.Core.Roots.Configs;

public interface ISyncConfigMerger
{
    string[] Editors { get; }

    object? GetMergedConfig(string root, string target);

    [Obsolete("Use GetDifferenceConfig(string root, string target, SyncFileMergeOptions options) instead. Will be removed in v19.")]
    object? GetDifferenceConfig(string root, string target)
        => GetDifferenceConfig(root, target, new SyncFileMergeOptions());

    object? GetDifferenceConfig(string root, string target, SyncFileMergeOptions options);
}
