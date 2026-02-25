using System.Collections.Generic;

namespace uSync.BackOffice;

/// <summary>
/// Bulk notification when Reporting is complete
/// </summary>
public class uSyncReportCompletedNotification : uSyncBulkNotification
{
    /// <inheritdoc/>
    public uSyncReportCompletedNotification(IEnumerable<uSyncAction> actions, string? group )
        : base(actions, group) { }


    /// <inheritdoc/>
    public uSyncReportCompletedNotification(IEnumerable<uSyncAction> actions)
        : base(actions, null) { }
}
