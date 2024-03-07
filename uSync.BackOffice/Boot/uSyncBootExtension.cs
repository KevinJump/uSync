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

namespace uSync.BackOffice.Boot;

/// <summary>
///  replaces the default 'NoNodes' page with a uSync one, which will allow us to 
///  tell the dev that the new site they have has got uSync files they can import
/// </summary>
internal static class uSyncBootExtension
{
    internal static string _defaultNoNodesPath = "~/umbraco/UmbracoWebsite/NoNodes.cshtml";
    internal static string _noNodesPath = "~/App_Plugins/uSync/boot/NoNodes.cshtml";

    public static IUmbracoBuilder AdduSyncFirstBoot(this IUmbracoBuilder builder)
    {
        builder.Services.PostConfigure<GlobalSettings>(settings =>
        {
            if (settings.NoNodesViewPath.InvariantEquals(_defaultNoNodesPath))
            {
                // if the default hasn't changed, put in the uSync version
                settings.NoNodesViewPath = _noNodesPath;
            }
        });

        // add notification handler to do the actual first boot run. 
        builder.AddNotificationHandler<UmbracoApplicationStartedNotification, FirstBootAppStartingHandler>();

        return builder;
    }
}

/// <summary>
///  Handler to mange app starting for first boot migrations 
/// </summary>
internal class FirstBootAppStartingHandler
    : INotificationHandler<UmbracoApplicationStartedNotification>
{

    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IKeyValueService _keyValueService;
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly IRuntimeState _runtimeState;

    /// <inheritdoc/>
    public FirstBootAppStartingHandler(
        ICoreScopeProvider scopeProvider,
        IKeyValueService keyValueService,
        IMigrationPlanExecutor migrationPlanExecutor,
        IRuntimeState runtimeState)
    {
        _scopeProvider = scopeProvider;
        _keyValueService = keyValueService;
        _migrationPlanExecutor = migrationPlanExecutor;
        _runtimeState = runtimeState;
    }


    /// <inheritdoc/>
    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        if (_runtimeState.Level == Umbraco.Cms.Core.RuntimeLevel.Run)
        {
            var firstBootMigration = new FirstBootMigrationPlan();
            var upgrader = new Upgrader(firstBootMigration);

            // this bit is done inside the upgrader.Execute method too, 
            // but we don't want the extra three log messages during startup,
            // so we also check before we start
            var currentState = _keyValueService.GetValue(upgrader.StateValueKey);
            if (currentState == null || currentState != firstBootMigration.FinalState)
                upgrader.Execute(_migrationPlanExecutor, _scopeProvider, _keyValueService);
        }
    }
}
