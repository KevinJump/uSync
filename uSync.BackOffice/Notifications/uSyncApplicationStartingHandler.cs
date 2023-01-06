using System;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Notifications
{
    /// <summary>
    ///  Run uSync tasks when the site has started up. 
    /// </summary>
    internal class uSyncApplicationStartingHandler : INotificationHandler<UmbracoApplicationStartedNotification>
    {
        private readonly ILogger<uSyncApplicationStartingHandler> _logger;
        private readonly IRuntimeState _runtimeState;
        private readonly IServerRoleAccessor _serverRegistrar;
        private readonly IUmbracoContextFactory _umbracoContextFactory;
        private readonly uSyncConfigService _uSyncConfig;
        private readonly SyncFileService _syncFileService;
        private readonly uSyncService _uSyncService;
        
        /// <summary>
        /// Generate a new uSyncApplicationStartingHandler object
        /// </summary>
        public uSyncApplicationStartingHandler(
            ILogger<uSyncApplicationStartingHandler> logger,
            IRuntimeState runtimeState,
            IServerRoleAccessor serverRegistrar,
            IUmbracoContextFactory umbracoContextFactory,
            uSyncConfigService uSyncConfigService,
            SyncFileService syncFileService,
            uSyncService uSyncService)
        {
            _runtimeState = runtimeState;
            _serverRegistrar = serverRegistrar;

            _umbracoContextFactory = umbracoContextFactory;

            _logger = logger;

            _uSyncConfig = uSyncConfigService;

            _syncFileService = syncFileService;
            _uSyncService = uSyncService;
        }

        /// <summary>
        ///  Handle the application starting notification event.
        /// </summary>
        public void Handle(UmbracoApplicationStartedNotification notification)
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

            InituSync();
        }

        /// <summary>
        ///  Initialize uSync elements (run start up import etc).
        /// </summary>
        private void InituSync()
        {
            var sw = Stopwatch.StartNew();

            try
            {
                using (var reference = _umbracoContextFactory.EnsureUmbracoContext())
                {
                    if (IsExportAtStartupEnabled() || (IsExportOnSaveOn() && !_syncFileService.RootExists(_uSyncConfig.GetRootFolder())))
                    {

                        var options = new SyncHandlerOptions
                        {
                            Group = _uSyncConfig.Settings.ExportOnSave
                        };
                        
                        _logger.LogInformation("uSync: Running export at startup");
                        _uSyncService.Export(_uSyncConfig.GetRootFolder(), options);
                    }

                    if (IsImportAtStartupEnabled())
                    {
                        _logger.LogInformation("uSync: Running Import at startup {group}", _uSyncConfig.Settings.ImportAtStartup);

                        if (!HasStopFile(_uSyncConfig.GetRootFolder()))
                        {
                            _uSyncService.Import(_uSyncConfig.GetRootFolder(), false, new SyncHandlerOptions
                            {
                                Group = _uSyncConfig.Settings.ImportAtStartup
                            });

                            ProcessOnceFile(_uSyncConfig.GetRootFolder());
                        }
                        else
                        {
                            _logger.LogInformation("Startup Import blocked by usync.stop file");
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
        ///  does the uSync folder contain a uSync.stop file (which would mean we would not process anything at startup)
        /// </summary>
        private bool HasStopFile(string folder)
            => _syncFileService.FileExists($"{folder}/usync.stop");

        /// <summary>
        ///  Process the once file (if it exists we rename it to usync.stop).
        /// </summary>
        private void ProcessOnceFile(string folder)
        {
            if (_syncFileService.FileExists($"{folder}/usync.once"))
            {
                _syncFileService.DeleteFile($"{folder}/usync.once");
                _syncFileService.SaveFile($"{folder}/usync.stop", "uSync Stop file, prevents startup import");
                _logger.LogInformation("usync.once file replaced by usync.stop file");
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
}
