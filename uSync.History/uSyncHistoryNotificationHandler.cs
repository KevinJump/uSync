using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Infrastructure.Serialization;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;

namespace uSync.History
{
    internal class uSyncHistoryNotificationHandler : INotificationHandler<uSyncImportCompletedNotification>
    {
        private readonly uSyncConfigService _configService;
        private readonly SyncFileService _syncFileService;

        public uSyncHistoryNotificationHandler(uSyncConfigService configService, SyncFileService syncFileService)
        {
            _configService = configService;
            _syncFileService = syncFileService;
        }

        public void Handle(uSyncImportCompletedNotification notification)
        {
            var changeActions = notification.Actions
                .Where(x => x.Change > Core.ChangeType.NoChange && x.Change < Core.ChangeType.Hidden)
                .ToList();

            if (changeActions.Any())
            {
                var actionJson = JsonConvert.SerializeObject(changeActions, Formatting.Indented);
                SaveActions(actionJson);
            }
        }

        private void SaveActions(string actionJson)
        {
            var rootFolder = _syncFileService.GetAbsPath(_configService.GetRootFolder());
            var historyFile = Path.Combine(rootFolder,"..", "history", DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + ".json");
            _syncFileService.CreateFoldersForFile(historyFile);
            _syncFileService.SaveFile(historyFile, actionJson);
        }
    }

    public class uSyncHistoryComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AddNotificationHandler<uSyncImportCompletedNotification, uSyncHistoryNotificationHandler>();
        }
    }
}
