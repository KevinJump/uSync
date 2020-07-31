using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using Umbraco.Core.Configuration;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

using uSync8.BackOffice.Services;

namespace uSync8.HistoryView.Controllers
{
    [PluginController("uSync")]
    public class uSyncHistoryApiController : UmbracoAuthorizedApiController
    {
        private readonly SyncFileService syncFileService;
        private readonly string historyFolder; 

        public uSyncHistoryApiController(IGlobalSettings globalSettings, SyncFileService syncFileService)
        {
            historyFolder = Path.Combine(globalSettings.LocalTempPath, "usync", "history");
            this.syncFileService = syncFileService;
        }

        [HttpGet]
        public bool GetApi() => true;


        [HttpGet]
        public IEnumerable<SyncHistoryView> GetHistory()
        {
            return GetHistory(historyFolder);
        }

        private IEnumerable<SyncHistoryView> GetHistory(string folder)
        {
            var histories = new List<SyncHistoryView>();

            foreach(var historyFile in syncFileService.GetFiles(folder, "*.history"))
            {
                var content = syncFileService.LoadContent(historyFile);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var history = JsonConvert.DeserializeObject<SyncHistoryView>(content);
                    if (history != null)
                    {
                        histories.Add(history);
                    }
                }
            }

            foreach(var subFolder in syncFileService.GetDirectories(folder))
            {
                histories.AddRange(GetHistory(subFolder));
            }

            return histories.OrderByDescending(x => x.When);
        }

    }
}
