using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Security;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;

namespace uSync.History
{
    internal class uSyncHistoryNotificationHandler 
        : INotificationHandler<uSyncImportCompletedNotification>,
        INotificationHandler<uSyncExportCompletedNotification>
    {
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
        private readonly ILogger<uSyncHistoryNotificationHandler> _logger;
        private readonly uSyncHistoryService _historyService;

        public uSyncHistoryNotificationHandler(
            IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
            ILogger<uSyncHistoryNotificationHandler> logger,
            uSyncHistoryService historyService)
        {
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
            _logger = logger;
            _historyService = historyService;
        }

        public void Handle(uSyncImportCompletedNotification notification)
        {
            if (_historyService.IsHistoryEnabled() == false) return;
            var changeActions = notification.Actions
                .Where(x => x.Change > Core.ChangeType.NoChange && x.Change < Core.ChangeType.Hidden)
                .ToList();

            if (changeActions.Any())
            {
                SaveActions(changeActions, "Import", notification.Actions.Count());
            }
        }

        public void Handle(uSyncExportCompletedNotification notification)
        {
            if (_historyService.IsHistoryEnabled() == false) return;
            var changeActions = notification.Actions
                .Where(x => x.Change > Core.ChangeType.NoChange && x.Change < Core.ChangeType.Hidden)
                .ToList();

            if (changeActions.Any())
            {
                SaveActions(changeActions, "Export", notification.Actions.Count());
            }
        }

        private void SaveActions(IEnumerable<uSyncAction> actions, string method, int total)
        {
            try
            {
                var historyInfo = new HistoryInfo
                {
                    Actions = actions,
                    Date = DateTime.Now,
                    Username = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Username ?? "Background Process",
                    Method = method,
                    Total = total,
                    Changes = actions.CountChanges()
                };

                var historyJson = JsonConvert.SerializeObject(historyInfo, Formatting.Indented);

                _historyService.SaveHistoryFile(historyJson);
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save history.");
            }
        }
    }
}
