
using System;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Migrations;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers;

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
                .To<FirstBootMigration>("FirstBoot-Migration");
                // .To<LogViewerMigration>("Logviewer-Migration");
    }
}

/// <summary>
/// First boot Feature migration
/// </summary>
public class FirstBootMigration : MigrationBase
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly uSyncConfigService _uSyncConfig;
    private readonly uSyncService _uSyncService;
    private readonly ILogger<FirstBootMigration> _logger;

    /// <inheritdoc/>
    public FirstBootMigration(
        IMigrationContext context,
        IUmbracoContextFactory umbracoContextFactory,
        uSyncConfigService uSyncConfig,
        uSyncService uSyncService,
        ILogger<FirstBootMigration> logger) : base(context)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _uSyncConfig = uSyncConfig;
        _uSyncService = uSyncService;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override void Migrate()
    {
        // first boot migration. 
        try
        {

            if (!_uSyncConfig.Settings.ImportOnFirstBoot)
                return;

            var sw = Stopwatch.StartNew();
            var changes = 0;

            _logger.LogInformation("Import on First-boot Set - will import {group} handler groups",
                _uSyncConfig.Settings.FirstBootGroup);

            // if config service is set to import on first boot then this 
            // will let uSync do a first boot import 

            // not sure about context on migrations so will need to test
            // or maybe we fire something into a notification (or use a static)

            using (var reference = _umbracoContextFactory.EnsureUmbracoContext())
            {
                var results = _uSyncService.Import(_uSyncConfig.GetFolders(), false, new SyncHandlerOptions
                {
                    Group = _uSyncConfig.Settings.FirstBootGroup
                }, null);

                changes = results.CountChanges();
            };

            sw.Stop();
            _logger.LogInformation("uSync First boot complete {changes} changes in ({time}ms)",
                changes, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "uSync First boot failed {message}", ex.Message);
            throw;
        }
    }
}
