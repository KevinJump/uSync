using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Routing;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Controllers;
using uSync.BackOffice.Hubs;

namespace uSync.BackOffice.Notifications
{
    /// <summary>
    ///  Handles ServerVariablesParsing to inject the uSync variables into the Umbraco.Sys namespace in Javascript
    /// </summary>
    internal class uSyncServerVariablesHandler : INotificationHandler<ServerVariablesParsingNotification>
    {
        private readonly uSyncConfigService _uSyncConfig;
        private readonly LinkGenerator _linkGenerator;
        private readonly uSyncHubRoutes _uSyncHubRoutes;
        private readonly IBackOfficeSecurityAccessor _securityAccessor;

        /// <inheritdoc cref="INotificationHandler{TNotification}" />
        public uSyncServerVariablesHandler(LinkGenerator linkGenerator,
            uSyncConfigService uSyncConfigService,
            uSyncHubRoutes hubRoutes,
            IBackOfficeSecurityAccessor securityAccessor)
        {
            _linkGenerator = linkGenerator;
            _uSyncConfig = uSyncConfigService;
            _uSyncHubRoutes = hubRoutes;
            _securityAccessor = securityAccessor;
        }


        /// <inheritdoc/>
        public void Handle(ServerVariablesParsingNotification notification)
        {
            notification.ServerVariables.Add("uSync", new Dictionary<string, object>
            {
                { "uSyncService", _linkGenerator.GetUmbracoApiServiceBaseUrl<uSyncDashboardApiController>(controller => controller.GetApi()) },
                { "signalRHub",  _uSyncHubRoutes.GetuSyncHubRoute() },
                { "disabledDashboard", _uSyncConfig.Settings.DisableDashboard },
                { "showFileActions", ShowFileActions() }
            });
        }

        public bool ShowFileActions()
        {
            var user = _securityAccessor?.BackOfficeSecurity?.CurrentUser;
            if (user == null) return false;
            return user.Groups.Any(x => x.Alias.Equals(Constants.Security.AdminGroupAlias));
        }

    }
}
