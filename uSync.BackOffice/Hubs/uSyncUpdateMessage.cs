using System;
using System.Collections.Generic;

using uSync.BackOffice.Models;

namespace uSync.BackOffice.Hubs;

/// <summary>
/// update message sent via uSync to client
/// </summary>
public class uSyncUpdateMessage
{
    /// <summary>
    /// string message to display
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///  number of items processed
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    ///  total number of items we expect to process
    /// </summary>
    public int Total { get; set; }
}


/// <summary>
///  message sent at the end of a sync operation.
/// </summary>
public class SyncCompleteMessage
{
    /// <summary>
    ///  request Id for the sync operation
    /// </summary>
    public Guid RequestId { get; set; }
    
    /// <summary>
    ///  completion message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///  ran to completion without errors
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///  final actions of the process
    /// </summary>
    public IEnumerable<uSyncActionView> Actions { get; set; } = [];
}