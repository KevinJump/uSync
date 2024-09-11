using System.Collections.Generic;

namespace uSync.BackOffice.Models;

/// <summary>
/// Result of a series of actions performed by a controller 
/// </summary>
public class SyncActionResult
{
    /// <summary>
    /// Construct a new SyncActionResult object
    /// </summary>
    public SyncActionResult() { }

    /// <summary>
    /// Construct a new SyncActionResult object
    /// </summary>
    /// <param name="actions">list of actions to include</param>
    public SyncActionResult(List<uSyncAction> actions)
    {
        this.Actions = actions;
    }

    /// <summary>
    /// List of actions performed by process
    /// </summary>
    public List<uSyncAction> Actions { get; set; } = [];
}
