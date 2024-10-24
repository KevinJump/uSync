
using System;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Events;

namespace uSync.BackOffice.Services;

/// <inheritdoc/>
public class SyncEventService : ISyncEventService
{
    private readonly IEventAggregator _eventAggregator;

    /// <inheritdoc/>
    public SyncEventService(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    /// <inheritdoc/>
    public bool IsPaused { get; private set; }

    /// <inheritdoc/>
    public void Pause() => IsPaused = true;

    /// <inheritdoc/>
    public void UnPause() => IsPaused = false;

    /// <inheritdoc/>
    public uSyncImportPause ImportPause(bool pause)
        => new uSyncImportPause(this, pause);

    /// <inheritdoc/>
    public async Task<bool> FireBulkStartingAsync(CancelableuSyncBulkNotification bulkNotification)
    {
        await _eventAggregator.PublishCancelableAsync(bulkNotification);
        return bulkNotification.Cancel;
    }

    /// <inheritdoc/>
    public async Task FireBulkCompleteAsync(uSyncBulkNotification notification)
    {
        await _eventAggregator.PublishAsync(notification);
    }

    /// <inheritdoc/>
    public async Task<bool> FireItemStartingEventAsync<TObject>(CancelableuSyncItemNotification<TObject> notification)
    {
        await _eventAggregator.PublishCancelableAsync(notification);
        return notification.Cancel;
    }

    /// <inheritdoc/>
    public async Task FireItemCompletedEventAsync<TObject>(uSyncItemNotification<TObject> notification)
    {
        await _eventAggregator.PublishAsync(notification);
    }
}
