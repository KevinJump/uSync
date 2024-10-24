using System.Threading.Tasks;

namespace uSync.BackOffice.Services;

/// <summary>
///  handles the events and locks for uSync events. 
/// </summary>
/// <remarks>
///  stops us tripping up over uSync firing save events etc while importing
///  gives us one place to fire our notifications from 
/// </remarks>
public interface ISyncEventService
{
    /// <summary>
    ///  true if uSync is current paused from collection information. 
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    ///  trigger the uSync bulk complete event
    /// </summary>
    /// <param name="notification"></param>
    /// <returns></returns>
    Task FireBulkCompleteAsync(uSyncBulkNotification notification);

    /// <summary>
    ///  trigger the bulk starting event (can be cancelled)
    /// </summary>
    Task<bool> FireBulkStartingAsync(CancelableuSyncBulkNotification bulkNotification);

    /// <summary>
    ///  trigger the item completed event
    /// </summary>
    Task FireItemCompletedEventAsync<TObject>(uSyncItemNotification<TObject> notification);

    /// <summary>
    ///  trigger the starting event (can be cancelled)
    /// </summary>
    Task<bool> FireItemStartingEventAsync<TObject>(CancelableuSyncItemNotification<TObject> notification);

    /// <summary>
    ///  get an import pause object (pauses the import until it is disposed)
    /// </summary>
    /// <remarks>
    ///  you should wrap code that might trigger Umbraco events in using(var pause = _mutexService.ImportPause())
    ///  this will ensure that uSync doesn't then pickup the imports as new things and saves them to disk.
    /// </remarks>
    uSyncImportPause ImportPause(bool pause);

    /// <summary>
    ///  pause the uSync triggering process
    /// </summary>
    void Pause();

    /// <summary>
    ///  un-pause the uSync triggering process
    /// </summary>
    void UnPause();
}