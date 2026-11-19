using Jumoo.Json;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Security;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.History.Service;

namespace uSync.History
{
    internal class uSyncHistoryNotificationHandler 
        : INotificationAsyncHandler<uSyncImportCompletedNotification>,
        INotificationAsyncHandler<uSyncExportCompletedNotification>
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ISyncFileService _syncFileService;
        private readonly ISyncHistoryService _syncHistoryService;
        private readonly ISyncConfigService _syncConfigService;
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
        private readonly ILogger<uSyncHistoryNotificationHandler> _logger;

        public uSyncHistoryNotificationHandler(
            ISyncFileService syncFileService,
            IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
            IHostingEnvironment hostingEnvironment,
            ILogger<uSyncHistoryNotificationHandler> logger,
            ISyncHistoryService syncHistoryService,
            ISyncConfigService syncConfigService)
        {
            _syncFileService = syncFileService;
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _syncHistoryService = syncHistoryService;
            _syncConfigService = syncConfigService;
        }

        public async Task HandleAsync(uSyncImportCompletedNotification notification, CancellationToken cancellationToken)
        {
            if (_syncHistoryService.IsEnabled() is false) return;

            var changeActions = notification.Actions
                .Where(x => x.Change > Core.ChangeType.NoChange && x.Change < Core.ChangeType.Hidden)
                .ToList();

            if (changeActions.Any())
            {
                if (await SaveActions(changeActions, "Import", notification.Actions.Count()))
                    PruneHistory();
            }
        }

        public async Task HandleAsync(uSyncExportCompletedNotification notification, CancellationToken cancellationToken)
        {
            if (_syncHistoryService.IsEnabled() is false) return;

            var changeActions = notification.Actions
                .Where(x => x.Change > Core.ChangeType.NoChange && x.Change < Core.ChangeType.Hidden)
                .ToList();

            if (changeActions.Any())
            {
                if (await SaveActions(changeActions, "Export", notification.Actions.Count()))
                    PruneHistory();
            }
        }

        private async Task<bool> SaveActions(IEnumerable<uSyncAction> actions, string method, int total)
        {
            try
            {
                var historyInfo = new HistoryInfo
                {
                    Actions = actions.Select(x => x.AsActionView()),
                    Date = DateTime.Now,
                    Username = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Username ?? "Background Process",
                    Method = method,
                    Total = total,
                    Changes = actions.CountChanges()
                };

                var historyJson = historyInfo.SerializeJsonString(true);
                if (historyJson is null)
                {
                    _logger.LogWarning("Failed to serialize the history, not saving it.");
                    return;
                }

                var historyFolder = SyncHistoryService.GetHistoryFolder(_syncFileService, _hostingEnvironment);
                var historyFile = Path.Combine(historyFolder, DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + ".json");

                _syncFileService.CreateFoldersForFile(historyFile);

                await _syncFileService.SaveFileAsync(historyFile, historyJson);
                return true;
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save history.");
                return false;
            }
        }

        private void PruneHistory()
        {
            try
            {
                var settings = _syncConfigService.Settings;
                SyncHistoryRetention.Prune(
                    SyncHistoryService.GetHistoryFolder(_syncFileService, _hostingEnvironment),
                    settings.HistoryMaxAgeDays,
                    settings.HistoryMaxEntries,
                    DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to prune history.");
            }
        }
    }
}
