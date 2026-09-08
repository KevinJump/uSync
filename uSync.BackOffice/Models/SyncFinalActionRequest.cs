using System;
using System.Collections.Generic;

using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice.Models;

/// <summary>
///  request model for things that happen at the end of the process.
/// </summary>
public class SyncFinalActionRequest
{
    /// <summary>
    ///  request Id
    /// </summary>
    public required Guid RequestId { get; set; }

    /// <summary>
    ///  current handler action (e.g import, export)
    /// </summary>
    public HandlerActions HandlerAction { get; set; } = HandlerActions.None;

    /// <summary>
    ///  options for the action
    /// </summary>
    public required SyncActionOptions ActionOptions { get; set; }

    /// <summary>
    ///  list of the results for the process
    /// </summary>
    public IEnumerable<uSyncAction> Actions { get; set; } = [];

    /// <summary>
    ///  user of the person who triggered the process
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///  callbacks for signalR ui refreshing. 
    /// </summary>
    public uSyncCallbacks? Callbacks { get; set; }
}


/// <summary>
///  Request model sent at the start of the process.
/// </summary>
public class SyncStartActionRequest
{
    /// <summary>
    ///  request Id - used to key the elapsed-time timer for this run, so
    ///  concurrent runs (and the multiple HTTP calls of a single stepped run)
    ///  don't stomp on each other's start time.
    /// </summary>
    public Guid? RequestId { get; set; }

    /// <summary>
    ///  current handler action (e.g import, export)
    /// </summary>
    public HandlerActions HandlerAction { get; set; } = HandlerActions.None;

    /// <summary>
    ///  user of the person who triggered the process
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///  should the folder be cleaned before the action(export) runs?
    /// </summary>
    public bool Clean { get; set; }
}