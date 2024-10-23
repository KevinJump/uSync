
using System;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Events;

namespace uSync.BackOffice.Services;

/// <summary>
///  handles the events and locks for uSync events. 
/// </summary>
/// <remarks>
///  stops us tripping up over uSync firing save events etc while importing
///  gives us one place to fire our notifications from 
/// </remarks>
public class uSyncEventService
{
    private readonly IEventAggregator _eventAggregator;

    /// <summary>
    ///  generate a new uSyncEventService object
    /// </summary>
    /// <param name="eventAggregator"></param>
    public uSyncEventService(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    /// <summary>
    ///  is uSync paused or not ? 
    /// </summary>
    public bool IsPaused { get; private set; }


    /// <summary>
    ///  pause the uSync triggering process
    /// </summary>
    public void Pause() => IsPaused = true;

    /// <summary>
    ///  un-pause the uSync triggering process
    /// </summary>
    public void UnPause() => IsPaused = false;

    /// <summary>
    ///  get an import pause object (pauses the import until it is disposed)
    /// </summary>
    /// <remarks>
    ///  you should wrap code that might trigger Umbraco events in using(var pause = _mutexService.ImportPause())
    ///  this will ensure that uSync doesn't then pickup the imports as new things and saves them to disk.
    /// </remarks>
    public uSyncImportPause ImportPause(bool pause)
        => new uSyncImportPause(this, pause);



    //// notification events. 
    ///

    internal async Task<bool> FireBulkStartingAsync(CancelableuSyncBulkNotification bulkNotification)
    {
        await _eventAggregator.PublishCancelableAsync(bulkNotification);
        return bulkNotification.Cancel;
    }

    internal async Task FireBulkCompleteAsync(uSyncBulkNotification notification)
    {
        await _eventAggregator.PublishAsync(notification);
    }

    internal async Task<bool> FireItemStartingEventAsync<TObject>(CancelableuSyncItemNotification<TObject> notification)
    {
        await _eventAggregator.PublishCancelableAsync(notification);
        return notification.Cancel;
    }

    internal async Task FireItemCompletedEventAsync<TObject>(uSyncItemNotification<TObject> notification)
    {
        await _eventAggregator.PublishAsync(notification);
    }



}
