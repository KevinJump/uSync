using Newtonsoft.Json;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Delete;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;

namespace uSync.History.Controllers
{
    [PluginController("uSync")]
    public class uSyncHistoryController : UmbracoAuthorizedApiController
    {
        private readonly uSyncConfigService _configService;
        private readonly SyncFileService _syncFileService;

        public uSyncHistoryController(uSyncConfigService configService, SyncFileService syncFileService)
        {
            _configService = configService;
            _syncFileService = syncFileService;
        }

        public IEnumerable<HistoryInfo> GetHistory()
        {
            var rootFolder = _syncFileService.GetAbsPath(_configService.GetRootFolder());
            var historyFolder = Path.GetFullPath(Path.Combine(rootFolder, "..", "history"));
            var files = _syncFileService.GetFiles(historyFolder, "*.json")
                .Select(x => x.Substring(historyFolder.Length + 1));

            var list = new List<HistoryInfo>();
            foreach (var file in files)
            {
                list.Add(LoadHistory(file));
            }

            return list.OrderByDescending(x => x.Date);
        }

        public bool ClearHistory()
        {
            // 1. get history folder
            var rootFolder = _syncFileService.GetAbsPath(_configService.GetRootFolder());
            var historyFolder = Path.GetFullPath(Path.Combine(rootFolder, "..", "history"));
            // 2. get history files
            var files = _syncFileService.GetFiles(historyFolder, "*.json");
            // 3. delet this
            foreach (var file in files)
            {
                _syncFileService.DeleteFile(file);
            }
            // 4. truth
            return true;
        }

        public HistoryInfo LoadHistory(string filePath)
        {
            var rootFolder = _syncFileService.GetAbsPath(_configService.GetRootFolder());
            var historyFolder = Path.GetFullPath(Path.Combine(rootFolder, "..", "history"));
            var fullPath = Path.Combine(historyFolder, filePath);
            string contents = _syncFileService.LoadContent(fullPath);

            var actions = JsonConvert.DeserializeObject<HistoryInfo>(contents);

            actions.FilePath = filePath;

            return actions;
        }
    }
}
