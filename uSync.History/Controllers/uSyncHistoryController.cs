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

        public readonly uSyncHistoryService _historyService;

        public uSyncHistoryController(uSyncHistoryService historyService)
        {
            _historyService = historyService;
        }

        public bool GetApi() => true;

        public IEnumerable<HistoryInfo> GetHistory()
        {
            return _historyService.GetHistory();
        }

        private string GetHistoryFolder()
        {
            return _historyService.GetHistoryFolder();
        }

        public bool ClearHistory()
        {
            return _historyService.ClearHistory();
        }

        public HistoryInfo LoadHistory(string filePath)
        {
            return _historyService.LoadHistory(filePath);
        }
    }
}
