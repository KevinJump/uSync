using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;
using Constants = Umbraco.Core.Constants;

namespace uSync8.BackOffice.Controllers
{
    [PluginController("uSync")]
    [UmbracoApplicationAuthorize(Constants.Applications.Settings)]
    public class uSyncDashboardApiController : UmbracoAuthorizedApiController
    {
        private readonly uSyncBackOfficeSettings settings;

        public uSyncDashboardApiController(uSyncBackOfficeSettings settings)
        {
            this.settings = settings;
        }

        [HttpGet]
        public uSyncBackOfficeSettings GetSettings()
        {
            return settings;
        }

    }
}
