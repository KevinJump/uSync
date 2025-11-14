using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.Core.Roots.Models;

namespace uSync.BackOffice.Models;

/// <summary>
///  options to pass to the merge process.
/// </summary>
/// <remarks>
///  the merge process takes multiple usync folders for a handler and merges them into
///  one 'virtual' folder structure, this is then used in the uSync processes. (roots)
/// </remarks>
public class SyncMergeOptions
{
    /// <summary>
    ///  Create a new instance of the SyncMergeOptions class
    /// </summary>
    public SyncMergeOptions() { }

    /// <summary>
    ///  new instance with the callback value set.
    /// </summary>
    public SyncMergeOptions(SyncUpdateCallback? callback)
    {
        UpdateCallback = callback;
    }

    /// <summary>
    ///  Callback use to pass info to the UI.
    /// </summary>
    public SyncUpdateCallback? UpdateCallback { get; set; }

    /// <summary>
    ///  what type of merging are we going to do.
    /// </summary>
    public SyncMergeStrategy MergeStrategy { get; set; } = SyncMergeStrategy.Magic;
}
