using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice.Notifications;

/// <summary>
///  Run uSync tasks when the site has started up. 
/// </summary>
internal class uSyncApplicationStartingHandler : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
{
    private readonly ILogger<uSyncApplicationStartingHandler> _logger;
    private readonly IRuntimeState _runtimeState;
    private readonly IServerRoleAccessor _serverRegistrar;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly ISyncConfigService _uSyncConfig;
    private readonly ISyncFileService _syncFileService;
    private readonly ISyncService _uSyncService;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;

    /// <summary>
    /// Generate a new uSyncApplicationStartingHandler object
    /// </summary>
    public uSyncApplicationStartingHandler(
        ILogger<uSyncApplicationStartingHandler> logger,
        IRuntimeState runtimeState,
        IServerRoleAccessor serverRegistrar,
        IUmbracoContextFactory umbracoContextFactory,
        ISyncConfigService uSyncConfigService,
        ISyncFileService syncFileService,
        ISyncService uSyncService,
        IBackgroundTaskQueue backgroundTaskQueue)
    {
        _runtimeState = runtimeState;
        _serverRegistrar = serverRegistrar;

        _umbracoContextFactory = umbracoContextFactory;

        _logger = logger;

        _uSyncConfig = uSyncConfigService;

        _syncFileService = syncFileService;
        _uSyncService = uSyncService;
        _backgroundTaskQueue = backgroundTaskQueue;
    }

    /// <summary>
    ///  Handle the application starting notification event.
    /// </summary>
    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
    {
        // we only run uSync when the site is running, and we 
        // are not running on a replica.
        if (_runtimeState.Level < RuntimeLevel.Run)
        {
            _logger.LogInformation("Umbraco is in {mode} mode, so uSync will not run this time.", _runtimeState.Level);
            return;
        }

        if (_serverRegistrar.CurrentServerRole == ServerRole.Subscriber)
        {
            _logger.LogInformation("This is a replicate server in a load balanced setup - uSync will not run {serverRole}", _serverRegistrar.CurrentServerRole);
            return;
        }
        
        if (_uSyncConfig.Settings.BackgroundStartup || _uSyncConfig.Settings.ProcessingMode == SyncProcessingMode.Background)
        {
            _logger.LogInformation("uSync: Running startup in background");
            _backgroundTaskQueue.QueueBackgroundWorkItem(
                cancellationToken =>
                {
                    using (ExecutionContext.SuppressFlow())
                    {
                        Task.Run(async () => await InituSyncAsync());
                        return Task.CompletedTask;
                    }
                });
        }
        else
        {
            await InituSyncAsync();
        }

    }

    /// <summary>
    ///  Initialize uSync elements (run start up import etc).
    /// </summary>
    private async Task InituSyncAsync()
    {
        var sw = Stopwatch.StartNew();

        try
        {
            using (var reference = _umbracoContextFactory.EnsureUmbracoContext())
            {
                if (IsExportAtStartupEnabled() || (IsExportOnSaveOn() && !HasSyncFolders()))
                {

                    var options = new SyncHandlerOptions
                    {
                        Group = _uSyncConfig.Settings.ExportOnSave
                    };

                    _logger.LogInformation("uSync: Running export at startup");
                    
                    
                    _uSyncService.StartupExportAsync(_uSyncConfig.GetWorkingFolder(), options).Wait();
                }

                if (IsImportAtStartupEnabled())
                {
                    _logger.LogInformation("uSync: Running Import at startup {group}", _uSyncConfig.Settings.ImportAtStartup);

                    if (!HasStopFile(_uSyncConfig.GetWorkingFolder()))
                    {
                        await _uSyncService.StartupImportAsync(_uSyncConfig.GetFolders(), false, new SyncHandlerOptions
                        {
                            Group = _uSyncConfig.Settings.ImportAtStartup
                        });

                        await ProcessOnceFileAsync(_uSyncConfig.GetWorkingFolder());
                    }
                    else
                    {
                        _logger.LogInformation("Startup Import blocked by {stopFile} file", _uSyncConfig.Settings.StopFile);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "uSync: Error during startup {message}", ex.Message);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("uSync: Startup Complete {elapsed}ms", sw.ElapsedMilliseconds);
        }

    }

    /// <summary>
    ///  checks if there are any of the sync folders (including root).
    /// </summary>
    /// <returns></returns>
    private bool HasSyncFolders()
    {
        foreach (var folder in _uSyncConfig.GetFolders())
        {
            if (_syncFileService.DirectoryExists(folder)) return true;
        }

        return false;
    }


    /// <summary>
    ///  does the uSync folder contain a uSync.stop file (which would mean we would not process anything at startup)
    /// </summary>
    private bool HasStopFile(string folder)
            => _syncFileService.FileExists($"{folder}/{_uSyncConfig.Settings.StopFile}");

    /// <summary>
    ///  Process the once file (if it exists we rename it to usync.stop).
    /// </summary>
    private async Task ProcessOnceFileAsync(string folder)
    {
        if (_syncFileService.FileExists($"{folder}/{_uSyncConfig.Settings.OnceFile}"))
        {
            _syncFileService.DeleteFile($"{folder}/{_uSyncConfig.Settings.OnceFile}");
            await _syncFileService.SaveFileAsync($"{folder}/{_uSyncConfig.Settings.StopFile}", "uSync Stop file, prevents startup import");
            _logger.LogInformation($"{_uSyncConfig.Settings.OnceFile} file replaced by {_uSyncConfig.Settings.StopFile} file");
        }
    }

    /// <summary>
    ///  is the export on save feature on (not blank or none)
    /// </summary>
    private bool IsExportOnSaveOn()
        => IsGroupSettingEnabled(_uSyncConfig.Settings.ExportOnSave);

    private bool IsImportAtStartupEnabled()
        => IsGroupSettingEnabled(_uSyncConfig.Settings.ImportAtStartup);

    private bool IsExportAtStartupEnabled()
        => IsGroupSettingEnabled(_uSyncConfig.Settings.ExportAtStartup);


    private bool IsGroupSettingEnabled(string value)
        => !string.IsNullOrWhiteSpace(value)
            && !value.InvariantEquals("none")
            && !value.InvariantEquals("off")
            && !value.InvariantEquals("false");
}
