
using Umbraco.Cms.Core.Events;

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
        INotificationHandler<uSyncExportCompletedNotification>
    {
        private readonly SyncEntityCache entityCache;

        public CacheLifecycleManager(SyncEntityCache entityCache)
        {
            this.entityCache = entityCache;
        }


        public void Handle(uSyncImportStartingNotification notification) => OnBulkActionComplete();

        public void Handle(uSyncExportCompletedNotification notification) => OnBulkActionComplete();

        public void Handle(uSyncImportCompletedNotification notification) => OnBulkActionComplete();

        public void Handle(uSyncExportStartingNotification notification) => OnBulkActionComplete();

        public void Handle(uSyncReportStartingNotification notification) => OnBulkActionComplete();

        public void Handle(uSyncReportCompletedNotification notification) => OnBulkActionComplete();

        private void OnBulkActionComplete()
        {
            entityCache.Clear();
        }
    }
}
