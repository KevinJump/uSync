using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Migrations;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice.Boot;

/// <summary>
/// Migration plan to add FirstBoot feature
/// </summary>
public class FirstBootMigrationPlan : MigrationPlan
{
    /// <inheritdoc/>
    public FirstBootMigrationPlan()
        : base("uSync_FirstBoot")
    {
        From(string.Empty)
                .To<FirstBootMigration>("FirstBoot-Migration")
                .To<LogViewerMigration>("LogViewer-Migration");
    }
}

/// <summary>
/// First boot Feature migration
/// </summary>
public class FirstBootMigration : UnscopedAsyncMigrationBase
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly ISyncConfigService _uSyncConfig;
    private readonly ISyncService _uSyncService;
    private readonly ILogger<FirstBootMigration> _logger;

    /// <inheritdoc/>
    public FirstBootMigration(
        IMigrationContext context,
        IUmbracoContextFactory umbracoContextFactory,
        ISyncConfigService uSyncConfig,
        ISyncService uSyncService,
        ILogger<FirstBootMigration> logger,
        IServerRoleAccessor serverRoleAccessor) : base(context)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _uSyncConfig = uSyncConfig;
        _uSyncService = uSyncService;
        _logger = logger;
        _serverRoleAccessor = serverRoleAccessor;
    }

    /// <inheritdoc/>
    protected override async Task MigrateAsync()
    {

        // first boot migration. 
        try
        {
            if (!_uSyncConfig.Settings.ImportOnFirstBoot)
                return;

            if (_serverRoleAccessor.CurrentServerRole == ServerRole.Subscriber)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("This is a Subscriber server in a load balanced setup - uSync only runs on single or schedulingPublisher (main) servers");
                
                return;
            }

            var sw = Stopwatch.StartNew();
            var changes = 0;

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Import on First-boot Set - will import {group} handler groups", _uSyncConfig.Settings.FirstBootGroup);

            // if config service is set to import on first boot then this 
            // will let uSync do a first boot import 

            // this runs as a 'un-scoped' migration, so we need to manage the context here.
            // as we are in the context and not just a scope all the publish stuff works 
            // first time, just like it does in ImportOnStartup 

            using (var reference = _umbracoContextFactory.EnsureUmbracoContext())
            {
                var results = await _uSyncService.StartupImportAsync(_uSyncConfig.GetFolders(), false, new SyncHandlerOptions
                {
                    Group = _uSyncConfig.Settings.FirstBootGroup
                });

                changes = results.CountChanges();
            };

            sw.Stop();
            
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("uSync First boot complete {changes} changes in ({time}ms)", changes, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "uSync First boot failed {message}", ex.Message);
        }
        finally
        {
            // we always complete the context - even if we fail.
            // we don't want to keep trying this migration every time.
            Context.Complete();
        }        
    }
}
