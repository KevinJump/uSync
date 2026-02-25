using System;
using System.Collections.Generic;

namespace uSync.BackOffice;

/// <summary>
///  bulk notification fired when import process is completed
/// </summary>
public class uSyncImportCompletedNotification : uSyncBulkNotification
{

    /// <inheritdoc/>
    public uSyncImportCompletedNotification(IEnumerable<uSyncAction> actions, string? group)
        : base(actions, group) { }

    /// <inheritdoc/>
    [Obsolete("Use the constructor with the group parameter instead will be removed in v19")]
    public uSyncImportCompletedNotification(IEnumerable<uSyncAction> actions)
        : base(actions, null) { }
}
