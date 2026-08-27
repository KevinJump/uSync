using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
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
    private readonly ILongRunningOperationService _longRunningOperationService;
    private readonly IServerRegistrationService _serverRegistrationService;

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
        ILongRunningOperationService longRunningOperationService,
        IServerRegistrationService serverRegistrationService)
    {
        _runtimeState = runtimeState;
        _serverRegistrar = serverRegistrar;

        _umbracoContextFactory = umbracoContextFactory;

        _logger = logger;

        _uSyncConfig = uSyncConfigService;

        _syncFileService = syncFileService;
        _uSyncService = uSyncService;
        _longRunningOperationService = longRunningOperationService;
        _serverRegistrationService = serverRegistrationService;
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
            if (_logger.IsEnabled(LogLevel.Information)) 
                _logger.LogInformation("Umbraco is in {mode} mode, so uSync will not run this time.", _runtimeState.Level);

            return;
        }

        if (_serverRegistrar.CurrentServerRole == ServerRole.Subscriber)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("This is a replicate server in a load balanced setup - uSync will not run {serverRole}", _serverRegistrar.CurrentServerRole);

            return;
        }

        WarnIfLoadBalancedWithoutBackgroundMode();

        // the role check above is NOT sufficient for a load-balanced backoffice:
        // Umbraco's documented setup for that topology pins every server to
        // SchedulingPublisher via a custom IServerRoleAccessor ("This will ensure
        // that all servers are treated as backoffice servers"), so on that setup
        // every server would otherwise run the startup import concurrently.
        // ILongRunningOperationService.RunAsync elects a single winner across the
        // cluster (it takes a DB write lock before checking for a running
        // operation of the same type), so wrapping the actual work in it makes
        // this safe regardless of topology - on a single server it just runs.
        var runInBackground = _uSyncConfig.Settings.BackgroundStartup || _uSyncConfig.Settings.ProcessingMode == SyncProcessingMode.Background;

        if (runInBackground && _logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("uSync: Running startup in background");

        var enqueueAttempt = await _longRunningOperationService.RunAsync(
            "uSync:Startup",
            _ => InituSyncAsync(),
            allowConcurrentExecution: false,
            runInBackground: runInBackground);

        if (enqueueAttempt.Success is false && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("uSync: Startup is already running (on this or another server) - skipping.");
        }
    }

    /// <summary>
    ///  Normal processing mode drives a run as a sequence of client requests that
    ///  rely on server-side state accumulated on whichever server handled the
    ///  previous request - that breaks the moment two requests for the same run
    ///  land on different servers, which is exactly what a load-balanced
    ///  backoffice (multiple active servers, no sticky sessions) does. There's no
    ///  reliable way to detect "the backoffice is load balanced" directly - every
    ///  server in that setup reports ServerRole.SchedulingPublisher by design -
    ///  so this uses the number of currently active servers as an honest proxy
    ///  instead, and only warns; it doesn't change behaviour.
    /// </summary>
    private void WarnIfLoadBalancedWithoutBackgroundMode()
    {
        if (_uSyncConfig.Settings.ProcessingMode != SyncProcessingMode.Normal) return;
        if (!_logger.IsEnabled(LogLevel.Warning)) return;

        try
        {
            var activeServers = _serverRegistrationService.GetActiveServers(refresh: false).Count();
            if (activeServers <= 1) return;

            _logger.LogWarning(
                "uSync is running in {mode} processing mode with {serverCount} active backoffice servers. " +
                "This mode does not work correctly behind a load-balanced backoffice - set uSync:Settings:ProcessingMode " +
                "to Background. See the background-processing-mode docs for details.",
                SyncProcessingMode.Normal, activeServers);
        }
        catch (Exception ex)
        {
            // this is a best-effort warning - never let it stop uSync starting.
            _logger.LogDebug(ex, "uSync: could not check active server count for the load-balancing warning.");
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

                    if (_logger.IsEnabled(LogLevel.Information))
                        _logger.LogInformation("uSync: Running export at startup");
                    
                    
                    _uSyncService.StartupExportAsync(_uSyncConfig.GetWorkingFolder(), options).Wait();
                }

                if (IsImportAtStartupEnabled())
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                        _logger.LogInformation("uSync: Running Import at startup {group}", _uSyncConfig.Settings.ImportAtStartup);

                    var workingFolder = _uSyncConfig.GetWorkingFolder();
                    var hasStopFile = HasStopFile(workingFolder);
                    var hasOnceFile = HasOnceFile(workingFolder);

                    if (ShouldProceedWithImport(hasStopFile, hasOnceFile))
                    {
                        await _uSyncService.StartupImportAsync(_uSyncConfig.GetFolders(), false, new SyncHandlerOptions
                        {
                            Group = _uSyncConfig.Settings.ImportAtStartup
                        });

                        await ProcessOnceFileAsync(workingFolder);
                    }
                    else
                    {
                        if (_logger.IsEnabled(LogLevel.Information))
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

            if (_logger.IsEnabled(LogLevel.Information))
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
    ///  Does the uSync folder contain a uSync.once file?
    /// </summary>
    private bool HasOnceFile(string folder)
            => _syncFileService.FileExists($"{folder}/{_uSyncConfig.Settings.OnceFile}");

    /// <summary>
    ///  Determines if the import should proceed based on the presence of stop and once files.
    /// </summary>
    /// <param name="hasStopFile">Whether a stop file exists in the working folder</param>
    /// <param name="hasOnceFile">Whether a once file exists in the working folder</param>
    /// <returns>True if import should proceed, false otherwise</returns>
    private bool ShouldProceedWithImport(bool hasStopFile, bool hasOnceFile)
    {
        // If there's no stop file, proceed with import
        if (!hasStopFile) return true;

        // If stop file exists and IgnoreStopIfOnceExists is enabled, check for once file
        if (_uSyncConfig.Settings.IgnoreStopIfOnceExists && hasOnceFile) return true;

        // Otherwise, stop file blocks the import
        return false;
    }

    /// <summary>
    ///  Process the once file (if it exists we rename it to usync.stop).
    /// </summary>
    private async Task ProcessOnceFileAsync(string folder)
    {
        if (_syncFileService.FileExists($"{folder}/{_uSyncConfig.Settings.OnceFile}"))
        {
            _syncFileService.DeleteFile($"{folder}/{_uSyncConfig.Settings.OnceFile}");
            await _syncFileService.SaveFileAsync($"{folder}/{_uSyncConfig.Settings.StopFile}", "uSync Stop file, prevents startup import");

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("{onceFile} file replaced by {stopFile} file", _uSyncConfig.Settings.OnceFile, _uSyncConfig.Settings.StopFile);
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
