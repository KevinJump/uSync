using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;
using uSync8.BackOffice.SyncHandlers;
using Constants = Umbraco.Core.Constants;

namespace uSync8.BackOffice.Controllers
{
    [PluginController("uSync")]
    [UmbracoApplicationAuthorize(Constants.Applications.Settings)]
    public class uSyncDashboardApiController : UmbracoAuthorizedApiController
    {
        private readonly uSyncBackOfficeSettings settings;
        private readonly SyncHandlerCollection syncHandlers;

        public uSyncDashboardApiController(
            SyncHandlerCollection syncHandlers,
            uSyncBackOfficeSettings settings)
        {
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
            return syncHandlers.Select(x => new
            {
                Name = x.Name,
                Enabled = x.Enabled,
                Icon = x.Icon,
                Type = x.GetType(),
                Folder = x.DefaultFolder,
                Priority = x.Priority
            }).OrderBy(x => x.Priority);
        }


        [HttpGet]
        public IEnumerable<uSyncAction> Report()
        {
            var updates = new List<uSyncAction>();

            foreach(var syncHandler in syncHandlers.Where(x => x.Enabled))
            {
                updates.AddRange(syncHandler.Report(syncHandler.DefaultFolder));
            }

            return updates;
        }
    }
}
