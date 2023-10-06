using Newtonsoft.Json;
using Umbraco.Cms.Core.Hosting;
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
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly SyncFileService _syncFileService;

        public uSyncHistoryController(SyncFileService syncFileService, IHostingEnvironment hostingEnvironment)
        {
            _syncFileService = syncFileService;
            _hostingEnvironment = hostingEnvironment;
        }

        public bool GetApi() => true;

        public IEnumerable<HistoryInfo> GetHistory()
        {
            string historyFolder = GetHistoryFolder();
            var files = _syncFileService.GetFiles(historyFolder, "*.json")
                .Select(x => x.Substring(historyFolder.Length + 1));

            var list = new List<HistoryInfo>();
            foreach (var file in files)
            {
                list.Add(LoadHistory(file));
            }

            return list.OrderByDescending(x => x.Date);
        }

        private string GetHistoryFolder()
        {
            var rootFolder = _syncFileService.GetAbsPath(_hostingEnvironment.LocalTempPath);
            var historyFolder = Path.GetFullPath(Path.Combine(rootFolder, "uSync", "history"));
            return historyFolder;
        }

        public bool ClearHistory()
        {
            // 1. get history folder
            string historyFolder = GetHistoryFolder();
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
            string historyFolder = GetHistoryFolder();
            var fullPath = Path.Combine(historyFolder, filePath);
            string contents = _syncFileService.LoadContent(fullPath);

            var actions = JsonConvert.DeserializeObject<HistoryInfo>(contents);

            actions.FilePath = filePath;

            return actions;
        }
    }
}
