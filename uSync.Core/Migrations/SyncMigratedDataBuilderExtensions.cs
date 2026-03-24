using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

using uSync.Core.Migrations.Migrations;
using uSync.Core.Persistance;

namespace uSync.Core.Migrations;

internal static class SyncMigratedDataBuilderExtensions
{
    public static IUmbracoBuilder AddSyncMigratedData(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<ISyncMigratedFullDataSetCachePolicy, SyncMigratedFullDataSetCachePolicy>();
        builder.Services.AddSingleton<ISyncMigratedDataRepository, SyncMigratedDataRepository>();
        builder.Services.AddSingleton<ISyncMigratedDataService, SyncMigratedDataService>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, SyncMigratedDataMigrationHandler>();

        return builder;
    }
}

internal class SyncMigratedDataMigrationHandler : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
{
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IKeyValueService _keyValueService;
    private readonly IRuntimeState _runtimeState;
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;

    public SyncMigratedDataMigrationHandler(
        ICoreScopeProvider scopeProvider,
        IKeyValueService keyValueService,
        IRuntimeState runtimeState,
        IMigrationPlanExecutor migrationPlanExecutor)
    {
        _scopeProvider = scopeProvider;
        _keyValueService = keyValueService;
        _runtimeState = runtimeState;
        _migrationPlanExecutor = migrationPlanExecutor;
    }

    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
    {
        // we don't run our migration until the site has been installed / isn't upgrading.
        if (_runtimeState.Level < RuntimeLevel.Run) return;

        var upgrader = new Upgrader(new SyncMigratedDataMigrationPlan());
        await upgrader.ExecuteAsync(_migrationPlanExecutor, _scopeProvider, _keyValueService);

    }
}