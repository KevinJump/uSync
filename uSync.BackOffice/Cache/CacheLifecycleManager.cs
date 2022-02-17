
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
        private readonly SyncEntityCache entityCache;
        private readonly ILogger<CacheLifecycleManager> logger;
        private readonly uSyncEventService eventService;

        public CacheLifecycleManager(
            ILogger<CacheLifecycleManager> logger,
            SyncEntityCache entityCache,
            uSyncEventService eventService)
        {
            this.logger = logger;
            this.entityCache = entityCache;
            this.eventService = eventService;
        }


        public void Handle(uSyncImportStartingNotification notification) => OnBulkActionComplete();

        public void Handle(uSyncExportCompletedNotification notification) => OnBulkActionComplete();

        public void Handle(uSyncImportCompletedNotification notification) => OnBulkActionComplete();

        public void Handle(uSyncExportStartingNotification notification) => OnBulkActionComplete();

        public void Handle(uSyncReportStartingNotification notification) => OnBulkActionComplete();

        public void Handle(uSyncReportCompletedNotification notification) => OnBulkActionComplete();

        public void Handle(ContentSavingNotification notification) => ClearOnEvents();
        public void Handle(ContentDeletingNotification notification) => ClearOnEvents();
        public void Handle(ContentMovingNotification notification) => ClearOnEvents();
        public void Handle(MediaSavingNotification notification) => ClearOnEvents();
        public void Handle(MediaSavedNotification notification) => ClearOnEvents();
        public void Handle(MediaDeletedNotification notification) => ClearOnEvents();

        private void OnBulkActionComplete()
        {
            entityCache.Clear();
        }

        private void ClearOnEvents()
        {
            try
            {
                if (eventService.IsPaused) return;
                entityCache.Clear();
            }
            catch(Exception ex)
            {
                logger.LogWarning(ex, "Failed to clean the entity name cache");
            }
        }
    }
}
