using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Controllers;
using uSync.BackOffice.Hubs;

namespace uSync.BackOffice.Notifications
{
    /// <summary>
    ///  Handles ServerVariablesParsing to inject the uSync variables into the Umbraco.Sys namespace in Javascript
    /// </summary>
    public class uSyncServerVariablesHandler : INotificationHandler<ServerVariablesParsingNotification>
    {
        private readonly uSyncConfigService _uSyncConfig;
        private readonly LinkGenerator _linkGenerator;
        private readonly uSyncHubRoutes _uSyncHubRoutes;

        [Obsolete("Will remove GlobalSettings & HostingEnvrionment in v11")]
        public uSyncServerVariablesHandler(LinkGenerator linkGenerator,
            UriUtility uriUtility,
            IOptions<GlobalSettings> globalSettings,
            uSyncConfigService uSyncConfigService,
            IHostingEnvironment hostingEnvironment,
            uSyncHubRoutes hubRoutes)
            : this(linkGenerator, uSyncConfigService, hubRoutes) { }


        /// <inheritdoc cref="INotificationHandler{TNotification}" />
        public uSyncServerVariablesHandler(LinkGenerator linkGenerator,
            uSyncConfigService uSyncConfigService,
            uSyncHubRoutes hubRoutes)
        {
            _linkGenerator = linkGenerator;
            _uSyncConfig = uSyncConfigService;
            _uSyncHubRoutes = hubRoutes;
        }


        /// <inheritdoc/>
        public void Handle(ServerVariablesParsingNotification notification)
        {
            notification.ServerVariables.Add("uSync", new Dictionary<string, object>
            {
                { "uSyncService", _linkGenerator.GetUmbracoApiServiceBaseUrl<uSyncDashboardApiController>(controller => controller.GetApi()) },
                { "signalRHub",  _uSyncHubRoutes.GetuSyncHubRoute() },
                { "disabledDashboard", _uSyncConfig.Settings.DisableDashboard }
            });
        }
    }
}
