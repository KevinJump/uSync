
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice.Services;
using uSync.Core.Cache;

namespace uSync.BackOffice.Cache;

/// <summary>
///  Cleans up the entity cache at start and end of the sync.
/// </summary>
public class CacheLifecycleManager :
    INotificationAsyncHandler<uSyncImportStartingNotification>,
    INotificationAsyncHandler<uSyncReportStartingNotification>,
    INotificationAsyncHandler<uSyncExportStartingNotification>,
    INotificationAsyncHandler<uSyncImportCompletedNotification>,
    INotificationAsyncHandler<uSyncReportCompletedNotification>,
    INotificationAsyncHandler<uSyncExportCompletedNotification>,
    INotificationAsyncHandler<ContentSavingNotification>,
    INotificationAsyncHandler<ContentDeletingNotification>,
    INotificationAsyncHandler<ContentMovingNotification>,
    INotificationAsyncHandler<MediaSavingNotification>,
    INotificationAsyncHandler<MediaSavedNotification>,
    INotificationAsyncHandler<MediaDeletedNotification>


{
    private readonly SyncEntityCache _entityCache;
    private readonly ILogger<CacheLifecycleManager> _logger;
    private readonly uSyncEventService _eventService;

    /// <summary>
    ///  Constructor
    /// </summary>
    public CacheLifecycleManager(
        ILogger<CacheLifecycleManager> logger,
        SyncEntityCache entityCache,
        uSyncEventService eventService)
    {
        _logger = logger;
        _entityCache = entityCache;
        _eventService = eventService;
    }


    /// <summary>
    ///  Handle the uSync import starting notification 
    /// </summary>
    public Task HandleAsync(uSyncImportStartingNotification notification, CancellationToken c) => OnBulkActionComplete();

    /// <summary>
    ///  Handle the uSync uSync Export Completed Notification 
    /// </summary>
    public Task HandleAsync(uSyncExportCompletedNotification notification, CancellationToken c) => OnBulkActionComplete();

    /// <summary>
    ///  Handle the uSync uSync Import Completed Notification 
    /// </summary>
    public Task HandleAsync(uSyncImportCompletedNotification notification, CancellationToken c) => OnBulkActionComplete();

    /// <summary>
    ///  Handle the uSync uSync Export Starting Notification 
    /// </summary>
    public Task HandleAsync(uSyncExportStartingNotification notification, CancellationToken c) => OnBulkActionComplete();

    /// <summary>
    ///  Handle the uSync uSync Report Completed Notification 
    /// </summary>
    public Task HandleAsync(uSyncReportStartingNotification notification, CancellationToken c) => OnBulkActionComplete();

    /// <summary>
    ///  Handle the uSync uSync Report Completed Notification 
    /// </summary>
    public Task HandleAsync(uSyncReportCompletedNotification notification, CancellationToken c) => OnBulkActionComplete();

    /// <summary>
    ///  Clear the cache on the Umbraco Content Saving notification 
    /// </summary>
    public Task HandleAsync(ContentSavingNotification notification, CancellationToken c) => ClearOnEvents();

    /// <summary>
    ///  Clear the cache on the Umbraco Content Deleting notification 
    /// </summary>
    public Task HandleAsync(ContentDeletingNotification notification, CancellationToken c) => ClearOnEvents();

    /// <summary>
    ///  Clear the cache on the Umbraco Content Moving notification 
    /// </summary>
    public Task HandleAsync(ContentMovingNotification notification, CancellationToken c) => ClearOnEvents();

    /// <summary>
    ///  Clear the cache on the Umbraco Media Saving notification 
    /// </summary>
    public Task HandleAsync(MediaSavingNotification notification, CancellationToken c) => ClearOnEvents();

    /// <summary>
    ///  Clear the cache on the Umbraco Media Saved notification 
    /// </summary>
    public Task HandleAsync(MediaSavedNotification notification, CancellationToken c) => ClearOnEvents();

    /// <summary>
    ///  Clear the cache on the Umbraco Media deleted notification 
    /// </summary>
    public Task HandleAsync(MediaDeletedNotification notification, CancellationToken c) => ClearOnEvents();

    private Task OnBulkActionComplete()
    {
        _entityCache.Clear();
        return Task.CompletedTask;
    }

    private Task ClearOnEvents()
    {
        try
        {
            if (_eventService.IsPaused) return Task.CompletedTask;
            _entityCache.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean the entity name cache");
        }

        return Task.CompletedTask;
    }
}
