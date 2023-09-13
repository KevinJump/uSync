
using Microsoft.Extensions.Logging;

using System;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice.Services;
using uSync.Core.Cache;

namespace uSync.BackOffice.Cache
{
    /// <summary>
    ///  Cleans up the entity cache at start and end of the sync.
    /// </summary>
    public class CacheLifecycleManager :
        INotificationHandler<uSyncImportStartingNotification>,
        INotificationHandler<uSyncReportStartingNotification>,
        INotificationHandler<uSyncExportStartingNotification>,
        INotificationHandler<uSyncImportCompletedNotification>,
        INotificationHandler<uSyncReportCompletedNotification>,
        INotificationHandler<uSyncExportCompletedNotification>,
        INotificationHandler<ContentSavingNotification>,
        INotificationHandler<ContentDeletingNotification>,
        INotificationHandler<ContentMovingNotification>,
        INotificationHandler<MediaSavingNotification>,
        INotificationHandler<MediaSavedNotification>,
        INotificationHandler<MediaDeletedNotification>


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
        public void Handle(uSyncImportStartingNotification notification) => OnBulkActionComplete();

        /// <summary>
        ///  Handle the uSync uSync Export Completed Notification 
        /// </summary>
        public void Handle(uSyncExportCompletedNotification notification) => OnBulkActionComplete();

        /// <summary>
        ///  Handle the uSync uSync Import Completed Notification 
        /// </summary>
        public void Handle(uSyncImportCompletedNotification notification) => OnBulkActionComplete();

        /// <summary>
        ///  Handle the uSync uSync Export Starting Notification 
        /// </summary>
        public void Handle(uSyncExportStartingNotification notification) => OnBulkActionComplete();

        /// <summary>
        ///  Handle the uSync uSync Report Completed Notification 
        /// </summary>
        public void Handle(uSyncReportStartingNotification notification) => OnBulkActionComplete();

        /// <summary>
        ///  Handle the uSync uSync Report Completed Notification 
        /// </summary>
        public void Handle(uSyncReportCompletedNotification notification) => OnBulkActionComplete();

        /// <summary>
        ///  Clear the cache on the Umbraco Content Saving notification 
        /// </summary>
        public void Handle(ContentSavingNotification notification) => ClearOnEvents();

        /// <summary>
        ///  Clear the cache on the Umbraco Content Deleting notification 
        /// </summary>
        public void Handle(ContentDeletingNotification notification) => ClearOnEvents();

        /// <summary>
        ///  Clear the cache on the Umbraco Content Moving notification 
        /// </summary>
        public void Handle(ContentMovingNotification notification) => ClearOnEvents();

        /// <summary>
        ///  Clear the cache on the Umbraco Media Saving notification 
        /// </summary>
        public void Handle(MediaSavingNotification notification) => ClearOnEvents();

        /// <summary>
        ///  Clear the cache on the Umbraco Media Saved notification 
        /// </summary>
        public void Handle(MediaSavedNotification notification) => ClearOnEvents();

        /// <summary>
        ///  Clear the cache on the Umbraco Media deleted notification 
        /// </summary>
        public void Handle(MediaDeletedNotification notification) => ClearOnEvents();

        private void OnBulkActionComplete()
        {
            _entityCache.Clear();
        }

        private void ClearOnEvents()
        {
            try
            {
                if (_eventService.IsPaused) return;
                _entityCache.Clear();
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean the entity name cache");
            }
        }
    }
}
