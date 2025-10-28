using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Web.Common.Filters;
using uSync.Backoffice.Management.Api.Configuration;
using uSync.BackOffice.Authorization;
using uSync.BackOffice.Services;

namespace uSync.History.Controllers
{
    [ApiController]
    [uSyncVersionedRoute("history")]
    [Authorize(Policy = SyncAuthorizationPolicies.TreeAccessuSync)]
    [MapToApi("uSync.History")]
    [DisableBrowserCache]
    [JsonOptionsName(Constants.JsonOptionsNames.BackOffice)]
    [ApiVersion("1.0")]
    [ApiExplorerSettings(GroupName = "History")]
    public class uSyncHistoryController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ISyncFileService _syncFileService;

        public uSyncHistoryController(ISyncFileService syncFileService, IHostingEnvironment hostingEnvironment)
        {
            _syncFileService = syncFileService;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet("GetHistory")]
        [ProducesResponseType(200)]
        public async Task<IEnumerable<HistoryInfo>> GetHistory()
        {
            string historyFolder = GetHistoryFolder();
            var files = _syncFileService.GetFiles(historyFolder, "*.json")
                .Select(x => x.Substring(historyFolder.Length + 1));

            var list = new List<HistoryInfo>();
            foreach (var file in files)
            {
                list.Add(await LoadHistoryAsync(file));
            }

            return list.OrderByDescending(x => x.Date);
        }

        [HttpGet("GetHistoryFolder")]
        [ProducesResponseType(200)]
        private string GetHistoryFolder()
        {
            var rootFolder = _syncFileService.GetAbsPath(_hostingEnvironment.LocalTempPath);
            var historyFolder = Path.GetFullPath(Path.Combine(rootFolder, "uSync", "history"));
            return historyFolder;
        }

        [HttpGet("ClearHistory")]
        [ProducesResponseType(200)]
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

        [HttpGet("HistoryInfo")]
        [ProducesResponseType(200)]
        public async Task<HistoryInfo> LoadHistoryAsync(string filePath)
        {
            string historyFolder = GetHistoryFolder();
            var fullPath = Path.Combine(historyFolder, filePath);
            string contents = await _syncFileService.LoadContentAsync(fullPath);

            var actions = JsonConvert.DeserializeObject<HistoryInfo>(contents);

            actions.FilePath = filePath;

            return actions;
        }
    }
}
