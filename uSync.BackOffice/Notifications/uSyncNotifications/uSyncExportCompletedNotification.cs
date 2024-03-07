using System.Collections.Generic;

namespace uSync.BackOffice;

/// <summary>
///  Notification object used when an bulk export has been completed
/// </summary>
public class uSyncExportCompletedNotification : uSyncBulkNotification
{

    /// <inheritdoc/>
    public uSyncExportCompletedNotification(IEnumerable<uSyncAction> actions)
        : base(actions) { }
}
