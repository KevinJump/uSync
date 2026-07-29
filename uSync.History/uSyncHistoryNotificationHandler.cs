using Jumoo.Json;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Security;

using uSync.BackOffice;
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
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
        private readonly ILogger<uSyncHistoryNotificationHandler> _logger;

        public uSyncHistoryNotificationHandler(
            ISyncFileService syncFileService,
            IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
            IHostingEnvironment hostingEnvironment,
            ILogger<uSyncHistoryNotificationHandler> logger,
            ISyncHistoryService syncHistoryService)
        {
            _syncFileService = syncFileService;
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _syncHistoryService = syncHistoryService;
        }

        public async Task HandleAsync(uSyncImportCompletedNotification notification, CancellationToken cancellationToken)
        {
            if (_syncHistoryService.IsEnabled() is false) return;

            var changeActions = notification.Actions
                .Where(x => x.Change > Core.ChangeType.NoChange && x.Change < Core.ChangeType.Hidden)
                .ToList();

            if (changeActions.Any())
            {
                await SaveActions(changeActions, "Import", notification.Actions.Count());
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
                await SaveActions(changeActions, "Export", notification.Actions.Count());
            }
        }

        private async Task SaveActions(IEnumerable<uSyncAction> actions, string method, int total)
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

                var rootFolder = _syncFileService.GetAbsPath(_hostingEnvironment.LocalTempPath);
                var historyFile = Path.Combine(rootFolder, "uSync", "history", DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + ".json");

                _syncFileService.CreateFoldersForFile(historyFile);

                await _syncFileService.SaveFileAsync(historyFile, historyJson);
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save history.");
            }
        }
    }
}
