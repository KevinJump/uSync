using Newtonsoft.Json;
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
        
        public IEnumerable<string> GetHistory()
        {
            var rootFolder = _syncFileService.GetAbsPath(_configService.GetRootFolder());
            var historyFolder = Path.GetFullPath(Path.Combine(rootFolder, "..", "history"));
            var files = _syncFileService.GetFiles(historyFolder, "*.json")
                .Select(x => x.Substring(historyFolder.Length + 1));

            return files;
        }

        public List<uSyncAction> LoadHistory(string filePath)
        {
            var rootFolder = _syncFileService.GetAbsPath(_configService.GetRootFolder());
            var historyFolder = Path.GetFullPath(Path.Combine(rootFolder, "..", "history"));
            var fullPath = Path.Combine(historyFolder, filePath);
            string contents = _syncFileService.LoadContent(fullPath);

            var actions = JsonConvert.DeserializeObject<List<uSyncAction>>(contents);

            return actions;
        }
    }
}
