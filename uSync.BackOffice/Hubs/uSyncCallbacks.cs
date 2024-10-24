using uSync.BackOffice.SyncHandlers.Interfaces;

using static uSync.BackOffice.ISyncService;

namespace uSync.BackOffice;

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
    ///  generate a new callback object 
    /// </summary>
    public uSyncCallbacks(SyncEventCallback? callback, SyncUpdateCallback? update)
    {
        this.Callback = callback;
        this.Update = update;
    }
}
