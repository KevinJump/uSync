using System.Collections.Generic;
using System.Linq;

using uSync.Core;
using uSync.Core.Models;

namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  Summary counts for a bulk import or export, sent in webhook payloads
/// </summary>
public class uSyncBulkWebhookSummary
{
    /// <summary>
    ///  total number of actions
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    ///  number of actions that changed something
    /// </summary>
    public int Changes { get; set; }

    /// <summary>
    ///  number of items created
    /// </summary>
    public int Created { get; set; }

    /// <summary>
    ///  number of items imported or updated
    /// </summary>
    public int Updated { get; set; }

    /// <summary>
    ///  number of items deleted
    /// </summary>
    public int Deleted { get; set; }

    /// <summary>
    ///  number of items exported
    /// </summary>
    public int Exported { get; set; }

    /// <summary>
    ///  number of items with no change
    /// </summary>
    public int NoChange { get; set; }

    /// <summary>
    ///  number of items that failed
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    ///  build the summary from a list of actions
    /// </summary>
    public static uSyncBulkWebhookSummary FromActions(IReadOnlyCollection<uSyncAction> actions)
        => new()
        {
            Total = actions.Count,
            Changes = actions.CountChanges(),
            Created = actions.Count(x => x.Success && x.Change == ChangeType.Create),
            Updated = actions.Count(x => x.Success && (x.Change == ChangeType.Import || x.Change == ChangeType.Update)),
            Deleted = actions.Count(x => x.Success && x.Change == ChangeType.Delete),
            Exported = actions.Count(x => x.Success && x.Change == ChangeType.Export),
            NoChange = actions.Count(x => x.Success && x.Change == ChangeType.NoChange),
            Failed = actions.CountErrors()
        };
}
