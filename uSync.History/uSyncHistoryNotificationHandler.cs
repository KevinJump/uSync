using Newtonsoft.Json;
using Umbraco.Cms.Core.Events;
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
        private readonly uSyncConfigService _configService;
        private readonly SyncFileService _syncFileService;
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

        public uSyncHistoryNotificationHandler(uSyncConfigService configService, SyncFileService syncFileService, IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        {
            _configService = configService;
            _syncFileService = syncFileService;
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        }

        public void Handle(uSyncImportCompletedNotification notification)
        {
            var changeActions = notification.Actions
                .Where(x => x.Change > Core.ChangeType.NoChange && x.Change < Core.ChangeType.Hidden)
                .ToList();

            if (changeActions.Any())
            {
                SaveActions(changeActions, "Import");
            }
        }

        public void Handle(uSyncExportCompletedNotification notification)
        {
            var changeActions = notification.Actions
                .Where(x => x.Change > Core.ChangeType.NoChange && x.Change < Core.ChangeType.Hidden)
                .ToList();

            if (changeActions.Any())
            {
                SaveActions(changeActions, "Export");
            }
        }

        private void SaveActions(IEnumerable<uSyncAction> actions, string method)
        {
            var historyInfo = new HistoryInfo
            {
                Actions = actions,
                Date = DateTime.Now,
                Username = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Username ?? "Background Process",
                Method = method
            };

            var historyJson = JsonConvert.SerializeObject(historyInfo, Formatting.Indented);

            var rootFolder = _syncFileService.GetAbsPath(_configService.GetRootFolder());
            var historyFile = Path.Combine(rootFolder, "..", "history", DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + ".json");

            _syncFileService.CreateFoldersForFile(historyFile);

            _syncFileService.SaveFile(historyFile, historyJson);
        }
    }
}
