using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

using uSync.Core.Migrations.Cache;
using uSync.Core.Migrations.Migrations;
using uSync.Core.Migrations.Notifications;

namespace uSync.Core.Migrations;

internal static class SyncMigratedDataBuilderExtensions
{
    public static IUmbracoBuilder AddSyncMigratedData(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<ISyncMigratedFullDataSetCachePolicy, SyncMigratedFullDataSetCachePolicy>();
        builder.Services.AddSingleton<ISyncMigratedDataRepository, SyncMigratedDataRepository>();
        builder.Services.AddSingleton<ISyncMigratedDataService, SyncMigratedDataService>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, SyncMigratedDataMigrationHandler>();
        builder.AddNotificationAsyncHandler<SyncExportCleanNotification, SyncExportCleanNotificationHandler>();

        return builder;
    }
}

internal class SyncMigratedDataMigrationHandler : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IKeyValueService _keyValueService;
    private readonly IRuntimeState _runtimeState;
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly ILogger<SyncMigratedDataMigrationHandler> _logger;

    public SyncMigratedDataMigrationHandler(
        ICoreScopeProvider scopeProvider,
        IKeyValueService keyValueService,
        IRuntimeState runtimeState,
        IMigrationPlanExecutor migrationPlanExecutor,
        ILogger<SyncMigratedDataMigrationHandler> logger)
    {
        _scopeProvider = scopeProvider;
        _keyValueService = keyValueService;
        _runtimeState = runtimeState;
        _migrationPlanExecutor = migrationPlanExecutor;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
    {
        // we don't run our migration until the site has been installed / isn't upgrading.
        if (_runtimeState.Level < RuntimeLevel.Run) return;

        // a slightly roundabout way of calling the migration plan
        var plan = new SyncMigratedDataMigrationPlan();
        var upgrader = new Upgrader(plan);

        // but here we can pre-check if the migration needs to happen.
        // and we reduce the amount of logging that appears at startup if it doesn't.
        var currentState = _keyValueService.GetValue(upgrader.StateValueKey);
        if (currentState == null || currentState != plan.FinalState)
        {
            await upgrader.ExecuteAsync(_migrationPlanExecutor, _scopeProvider, _keyValueService);
        }
        else
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("{migration} Migration skipped as it has already been completed in a previous run.", nameof(SyncMigratedDataMigrationPlan));
        }
    }
}