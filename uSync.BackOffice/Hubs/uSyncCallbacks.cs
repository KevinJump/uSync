using Org.BouncyCastle.Bcpg.OpenPgp;

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

public delegate void SyncSetUpdateRange(int start, int end);
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

    public SyncSetUpdateRange? SetRange { get; private set; }

    public SyncIncrementalUpdateCallback? IncrementalUpdate { get; private set; }

    /// <summary>
    ///  generate a new callback object 
    /// </summary>
    public uSyncCallbacks(SyncEventCallback? callback, SyncUpdateCallback? update)
    {
        this.Callback = callback;
        this.Update = update;
    }

    public uSyncCallbacks(SyncEventCallback? callback, SyncUpdateCallback? update, SyncSetUpdateRange? updateRange, SyncIncrementalUpdateCallback incrementalUpdate)
        : this(callback, update)
    {
        this.SetRange = updateRange;
        this.IncrementalUpdate = incrementalUpdate;
    }
}
