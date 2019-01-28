using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;
using uSync8.BackOffice.Hubs;
using uSync8.BackOffice.SyncHandlers;
using Constants = Umbraco.Core.Constants;

namespace uSync8.BackOffice.Controllers
{
    [PluginController("uSync")]
    [UmbracoApplicationAuthorize(Constants.Applications.Settings)]
    public class uSyncDashboardApiController : UmbracoAuthorizedApiController
    {
        private readonly uSyncService uSyncService;

        private readonly uSyncBackOfficeSettings settings;
        private readonly SyncHandlerCollection syncHandlers;

        public uSyncDashboardApiController(
            uSyncService uSyncService,
            SyncHandlerCollection syncHandlers,
            uSyncBackOfficeSettings settings)
        {
            this.uSyncService = uSyncService;
            this.settings = settings;
            this.syncHandlers = syncHandlers;
        }

        [HttpGet]
        public uSyncBackOfficeSettings GetSettings()
        {
            return settings;
        }

        [HttpGet]
        public IEnumerable<object> GetHandlers()
        {
            return syncHandlers.Select(x => new SyncHandlerSummary()
            {
                Icon = x.Icon,
                Name = x.Name,
                Status = HandlerStatus.Pending
            });
        }


        [HttpPost]
        public IEnumerable<uSyncAction> Report(uSyncOptions options)
        {
            var hubClient = new HubClientService(options.clientId);
            var summaryClient = new SummaryHandler(hubClient);

            return uSyncService.Report(settings.rootFolder, summaryClient.PostSummary);
        }

        [HttpPost]
        public IEnumerable<uSyncAction> Export(uSyncOptions options)
        {
            var hubClient = new HubClientService(options.clientId);
            var summaryClient = new SummaryHandler(hubClient);

            return uSyncService.Export(settings.rootFolder, summaryClient.PostSummary);
        }

        [HttpPut]
        public IEnumerable<uSyncAction> Import(uSyncOptions options)
        {
            var hubClient = new HubClientService(options.clientId);
            var summaryClient = new SummaryHandler(hubClient);

            return uSyncService.Import(settings.rootFolder, options.force, summaryClient.PostSummary);
        }

        public class SummaryHandler
        {
            private readonly HubClientService hubClient;

            public SummaryHandler(HubClientService hubClient)
            {
                this.hubClient = hubClient;
            }

            public void PostSummary(SyncProgressSummary summary)
            {
                hubClient.SendMessage(summary);
            }

        }

        public class uSyncOptions
        {
            public string clientId { get; set; }
            public bool force { get; set; }
            public bool clean { get; set; }
        }
    }
}
