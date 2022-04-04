
using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Extensions;

namespace uSync.BackOffice.Boot
{
    /// <summary>
    ///  replaces the default 'NoNodes' page with a uSync one, which will allow us to 
    ///  tell the dev that the new site they have has got uSync files they can import
    /// </summary>
    internal static class uSyncBootExtension
    {
        internal static string defaultNoNodesPath = "~/umbraco/UmbracoWebsite/NoNodes.cshtml";
        internal static string noNodesPath = "~/App_Plugins/uSync/boot/NoNodes.cshtml";

        public static IUmbracoBuilder AdduSyncFirstBoot(this IUmbracoBuilder builder)
        {
            builder.Services.PostConfigure<GlobalSettings>(settings =>
            {
                if (settings.NoNodesViewPath.InvariantEquals(defaultNoNodesPath))
                {
                    // if the default hasn't changed, put in the uSync version
                    settings.NoNodesViewPath = noNodesPath;
                }
            });

            // add notification handler to do the actual first boot run. 
            builder.AddNotificationHandler<UmbracoApplicationStartingNotification, FirstBootAppStartingHandler>();
           
            return builder;
        }
    }

    public class FirstBootAppStartingHandler
        : INotificationHandler<UmbracoApplicationStartingNotification>
    {

        private readonly IScopeProvider _scopeProvider;
        private readonly IKeyValueService _keyValueService;
        private readonly IMigrationPlanExecutor _migrationPlanExecutor;

        public FirstBootAppStartingHandler(
            IScopeProvider scopeProvider,
            IKeyValueService keyValueService,
            IMigrationPlanExecutor migrationPlanExecutor)
        {
            _scopeProvider = scopeProvider;
            _keyValueService = keyValueService;
            _migrationPlanExecutor = migrationPlanExecutor;
        }

        public void Handle(UmbracoApplicationStartingNotification notification)
        {
            if (notification.RuntimeLevel == Umbraco.Cms.Core.RuntimeLevel.Run)
            {
                var upgrader = new Upgrader(new FirstBootMigrationPlan());
                upgrader.Execute(_migrationPlanExecutor, _scopeProvider, _keyValueService);
            }
        }
    }
}
