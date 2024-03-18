using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;
using Umbraco.Extensions;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Hubs;
using uSync.History.Controllers;

namespace uSync.History
{
    public class uSyncHistoryComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<uSyncHistoryService>();

            builder.AddNotificationHandler<ServerVariablesParsingNotification, uSyncHistoryServerVariablesHandler>();
            builder.AddNotificationHandler<uSyncImportCompletedNotification, uSyncHistoryNotificationHandler>();
            builder.AddNotificationHandler<uSyncExportCompletedNotification, uSyncHistoryNotificationHandler>();
            // don't add if the filter is already there .
            if (!builder.ManifestFilters().Has<uSyncHistoryManifestFilter>())
            {
                // add the package manifest programatically. 
                builder.ManifestFilters().Append<uSyncHistoryManifestFilter>();
            }
        }
    }

    internal class uSyncHistoryServerVariablesHandler : INotificationHandler<ServerVariablesParsingNotification>
    {
        private readonly uSyncConfigService _uSyncConfig;
        private readonly LinkGenerator _linkGenerator;
        private readonly uSyncHubRoutes _uSyncHubRoutes;
        private readonly IBackOfficeSecurityAccessor _securityAccessor;

        /// <inheritdoc cref="INotificationHandler{TNotification}" />
        public uSyncHistoryServerVariablesHandler(LinkGenerator linkGenerator,
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
            notification.ServerVariables.Add("uSyncHistory", new Dictionary<string, object>
            {
                { "Enabled", _uSyncConfig.Settings.EnableHistory },
                { "Service", _linkGenerator.GetUmbracoApiServiceBaseUrl<uSyncHistoryController>(controller => controller.GetApi()) },
            });
        }
    }
}
