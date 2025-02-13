using System;
using System.Collections.Generic;

using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice.Models;

public class SyncFinalActionRequest
{
    public required Guid RequestId { get; set; }
    public HandlerActions HandlerAction { get; set; } = HandlerActions.None;
    public required SyncActionOptions ActionOptions { get; set; }
    public IEnumerable<uSyncAction> Actions { get; set; } = [];
    public string? Username { get; set; }
    public uSyncCallbacks? Callbacks { get; set; }
}

