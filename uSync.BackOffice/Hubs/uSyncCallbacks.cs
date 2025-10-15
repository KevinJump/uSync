using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers.Interfaces;

namespace uSync.BackOffice;

/// <summary>
///  Callback event for SignalR hub
/// </summary>
public delegate void SyncEventCallback(SyncProgressSummary summary);

/// <summary>
///  callback delegate for SignalR messaging 
/// </summary>
public delegate void SyncUpdateCallback(string message, int count, int total);

/// <summary>
///  callback delegate to set the start and end range for the update counters.
/// </summary>
public delegate void SyncSetUpdateRange(int start, int end);

/// <summary>
///  callback to send a update message and increment the counter by one so moving the progress bar.
/// </summary>
/// <param name="message"></param>
public delegate void SyncIncrementalUpdateCallback(string message);


/// <summary>
///  Callback objects used to communicate via SignalR
/// </summary>
public class uSyncCallbacks
{
    /// <summary>
    ///  Add event callback
    /// </summary>
    public SyncEventCallback? Callback { get; private set; }

    /// <summary>
    ///  Update event callback
    /// </summary>
    public SyncUpdateCallback? Update { get; private set; }

    /// <summary>
    ///  set a start and end range for the counter.
    /// </summary>
    public SyncSetUpdateRange? SetRange { get; private set; }

    /// <summary>
    ///  update and increment callback.
    /// </summary>
    public SyncIncrementalUpdateCallback? IncrementalUpdate { get; private set; }

    /// <summary>
    ///  generate a new callback object 
    /// </summary>
    public uSyncCallbacks(SyncEventCallback? callback, SyncUpdateCallback? update)
    {
        this.Callback = callback;
        this.Update = update;
    }

    /// <summary>
    ///  generate a callback object with range and incremental update
    /// </summary>
    public uSyncCallbacks(SyncEventCallback? callback, SyncUpdateCallback? update, SyncSetUpdateRange? updateRange, SyncIncrementalUpdateCallback incrementalUpdate)
        : this(callback, update)
    {
        this.SetRange = updateRange;
        this.IncrementalUpdate = incrementalUpdate;
    }
}
