using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

using uSync8.Core.Cache;

namespace uSync8.BackOffice.Cache
{
    /// <summary>
    ///  Cleans up the entity cache at start and end of the sync.
    /// </summary>
    public class CacheLifecycleManager
    {
        private readonly SyncEntityCache entityCache;
        private readonly ILogger logger;

        public CacheLifecycleManager(
            ILogger logger,
            SyncEntityCache entityCache)
        {
            this.entityCache = entityCache;
            this.logger = logger;
        }

        public void Intialize() { 

            uSyncService.ImportStarting += OnBulkActionComplete;
            uSyncService.ImportComplete += OnBulkActionComplete;

            uSyncService.ReportStarting += OnBulkActionComplete;
            uSyncService.ReportComplete += OnBulkActionComplete;

            uSyncService.ExportStarting += OnBulkActionComplete;
            uSyncService.ExportComplete += OnBulkActionComplete;

            ContentService.Saving += (s, e) => ClearOnEvent();
            ContentService.Deleting += (s, e) => ClearOnEvent();
            ContentService.Moving += (s, e) => ClearOnEvent();

            MediaService.Saving += (s, e) => ClearOnEvent();
            MediaService.Deleting += (s, e) => ClearOnEvent();
            MediaService.Moving += (s, e) => ClearOnEvent();
        }

        private void ClearOnEvent()
        {
            try
            {
                if (uSync8BackOffice.eventsPaused) return;
                
                entityCache.Clear();
            }
            catch (Exception ex)
            {
                logger.Warn<CacheLifecycleManager>(ex, "Failed to clean the entity name caches");
                // it really should never fail, but we don't want to block a save event just because our cache is not there.
            }
        }

        private void OnBulkActionComplete(uSyncBulkEventArgs e)
        {
            entityCache.Clear();
        }
    }
}
