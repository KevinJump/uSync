using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uSync8.Core.Cache;

namespace uSync8.BackOffice.Cache
{
    /// <summary>
    ///  Cleans up the entity cache at start and end of the sync.
    /// </summary>
    public class CacheLifecycleManager
    {
        private readonly SyncEntityCache entityCache;

        public CacheLifecycleManager(SyncEntityCache entityCache)
        {
            this.entityCache = entityCache;

            uSyncService.ImportStarting += OnBulkActionComplete;
            uSyncService.ImportComplete += OnBulkActionComplete;

            uSyncService.ReportStarting += OnBulkActionComplete;
            uSyncService.ReportComplete += OnBulkActionComplete;

            uSyncService.ExportStarting += OnBulkActionComplete;
            uSyncService.ExportComplete += OnBulkActionComplete;
        }

        private void OnBulkActionComplete(uSyncBulkEventArgs e)
        {
            entityCache.Clear();
        }
    }
}
