using System;
using System.Collections.Generic;

namespace uSync.BackOffice;

/// <summary>
///  Notification object used when an bulk export has been completed
/// </summary>
public class uSyncExportCompletedNotification : uSyncBulkNotification
{
    /// <inheritdoc/>
    public uSyncExportCompletedNotification(IEnumerable<uSyncAction> actions, string? group)
        : base(actions, group) { }

    /// <inheritdoc/>
    [Obsolete("Use the constructor with the group parameter instead will be removed in v19")]
    public uSyncExportCompletedNotification(IEnumerable<uSyncAction> actions)
        : base(actions, null) { }
}
