using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using uSync.BackOffice.Models;

namespace uSync.BackOffice;

/// <summary>
///  Callback event for SignalR hub
/// </summary>
public delegate void SyncEventCallback(SyncProgressSummary summary);

/// <summary>
///  async callback delegate for SignalR messaging
/// </summary>
public delegate Task SyncEventCallbackAsync(SyncProgressSummary summary);

/// <summary>
///  callback delegate for SignalR messaging
/// </summary>
public delegate void SyncUpdateCallback(string message, int count, int total);

/// <summary>
///  async callback delegate for SignalR messaging
/// </summary>
public delegate Task SyncUpdateCallbackAsync(string message, int count, int total);

/// <summary>
///  callback delegate to set the start and end range for the update counters.
/// </summary>
public delegate void SyncSetUpdateRange(int start, int end);

/// <summary>
///  async callback delegate to set the start and end range for the update counters.
/// </summary>
public delegate Task SyncSetUpdateRangeAsync(int start, int end);

/// <summary>
///  callback to send a update message and increment the counter by one so moving the progress bar.
/// </summary>
/// <param name="message"></param>
public delegate void SyncIncrementalUpdateCallback(string message);

/// <summary>
///  async callback to send a update message and increment the counter by one so moving the progress bar.
/// </summary>
/// <param name="message"></param>
public delegate Task SyncIncrementalUpdateCallbackAsync(string message);

/// <summary>
///  callback to signal that the sync is complete
/// </summary>
public delegate void SyncCompleteCallBack(Guid id, string message, bool success, IEnumerable<uSyncActionView> actions);

/// <summary>
///  async callback to signal that the sync is complete
/// </summary>
public delegate Task SyncCompleteCallBackAsync(Guid id, string message, bool success, IEnumerable<uSyncActionView> actions);


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
    ///  Async add event callback. Set alongside (or instead of) <see cref="Callback"/> when the
    ///  consumer needs to await the send (e.g. a SignalR hub push) rather than block on it.
    /// </summary>
    public SyncEventCallbackAsync? CallbackAsync { get; set; }

    /// <summary>
    ///  Update event callback
    /// </summary>
    public SyncUpdateCallback? Update { get; private set; }

    /// <summary>
    ///  Async update event callback. Set alongside (or instead of) <see cref="Update"/> when the
    ///  consumer needs to await the send rather than block on it.
    /// </summary>
    public SyncUpdateCallbackAsync? UpdateAsync { get; set; }

    /// <summary>
    ///  set a start and end range for the counter.
    /// </summary>
    public SyncSetUpdateRange? SetRange { get; private set; }

    /// <summary>
    ///  Async version of <see cref="SetRange"/>.
    /// </summary>
    public SyncSetUpdateRangeAsync? SetRangeAsync { get; set; }

    /// <summary>
    ///  update and increment callback.
    /// </summary>
    public SyncIncrementalUpdateCallback? IncrementalUpdate { get; private set; }

    /// <summary>
    ///  Async version of <see cref="IncrementalUpdate"/>.
    /// </summary>
    public SyncIncrementalUpdateCallbackAsync? IncrementalUpdateAsync { get; set; }

    /// <summary>
    ///  callback to signal that the sync is complete
    /// </summary>
    public SyncCompleteCallBack? Complete { get; private set; }

    /// <summary>
    ///  Async version of <see cref="Complete"/>.
    /// </summary>
    public SyncCompleteCallBackAsync? CompleteAsync { get; set; }


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
    public uSyncCallbacks(SyncEventCallback? callback,
        SyncUpdateCallback? update,
        SyncSetUpdateRange? updateRange,
        SyncIncrementalUpdateCallback incrementalUpdate,
        SyncCompleteCallBack? complete)
        : this(callback, update)
    {
        this.SetRange = updateRange;
        this.IncrementalUpdate = incrementalUpdate;
        this.Complete = complete;
    }

    /// <summary>
    ///  raise the <see cref="Callback"/> / <see cref="CallbackAsync"/> event, awaiting the async
    ///  version (if set) so callers no longer need to block on it.
    /// </summary>
    public async Task RaiseCallbackAsync(SyncProgressSummary summary)
    {
        Callback?.Invoke(summary);
        if (CallbackAsync is not null)
            await CallbackAsync(summary).ConfigureAwait(false);
    }

    /// <summary>
    ///  raise the <see cref="Update"/> / <see cref="UpdateAsync"/> event, awaiting the async
    ///  version (if set) so callers no longer need to block on it.
    /// </summary>
    public async Task RaiseUpdateAsync(string message, int count, int total)
    {
        Update?.Invoke(message, count, total);
        if (UpdateAsync is not null)
            await UpdateAsync(message, count, total).ConfigureAwait(false);
    }

    /// <summary>
    ///  raise the <see cref="SetRange"/> / <see cref="SetRangeAsync"/> event, awaiting the async
    ///  version (if set) so callers no longer need to block on it.
    /// </summary>
    public async Task RaiseSetRangeAsync(int start, int end)
    {
        SetRange?.Invoke(start, end);
        if (SetRangeAsync is not null)
            await SetRangeAsync(start, end).ConfigureAwait(false);
    }

    /// <summary>
    ///  raise the <see cref="IncrementalUpdate"/> / <see cref="IncrementalUpdateAsync"/> event, awaiting
    ///  the async version (if set) so callers no longer need to block on it.
    /// </summary>
    public async Task RaiseIncrementalUpdateAsync(string message)
    {
        IncrementalUpdate?.Invoke(message);
        if (IncrementalUpdateAsync is not null)
            await IncrementalUpdateAsync(message).ConfigureAwait(false);
    }

    /// <summary>
    ///  raise the <see cref="Complete"/> / <see cref="CompleteAsync"/> event, awaiting the async
    ///  version (if set) so callers no longer need to block on it.
    /// </summary>
    public async Task RaiseCompleteAsync(Guid id, string message, bool success, IEnumerable<uSyncActionView> actions)
    {
        Complete?.Invoke(id, message, success, actions);
        if (CompleteAsync is not null)
            await CompleteAsync(id, message, success, actions).ConfigureAwait(false);
    }
}
