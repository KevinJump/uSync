using System;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
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
    public class uSyncApplicationStartingHandler : INotificationHandler<UmbracoApplicationStarting>
    {
        private ILogger<uSyncApplicationStartingHandler> _logger;

        private IRuntimeState _runtimeState;
        private IServerRoleAccessor _serverRegistrar;

        private IUmbracoContextFactory _umbracoContextFactory;

        private readonly uSyncConfigService _uSyncConfig;

        private SyncFileService _syncFileService;
        private uSyncService _uSyncService;

        public uSyncApplicationStartingHandler(
            ILogger<uSyncApplicationStartingHandler> logger,
            IRuntimeState runtimeState,
            IServerRoleAccessor serverRegistrar,
            IUmbracoContextFactory umbracoContextFactory,
            uSyncConfigService uSyncConfigService,
            SyncFileService syncFileService,
            uSyncService uSyncService)
        {
            this._runtimeState = runtimeState;
            this._serverRegistrar = serverRegistrar;

            this._umbracoContextFactory = umbracoContextFactory;

            this._logger = logger;

            this._uSyncConfig = uSyncConfigService;

            this._syncFileService = syncFileService;
            this._uSyncService = uSyncService;
        }

        /// <summary>
        ///  Handle the appliction starting notification event.
        /// </summary>
        public void Handle(UmbracoApplicationStarting notification)
        {
            /// we only run uSync when the site is running, and we 
            /// are not running on a replica.
            if (_runtimeState.Level < RuntimeLevel.Run)
            {
                _logger.LogInformation("Umbaco is not in Run mode, so uSync will not run");
                return;
            }

            if (_serverRegistrar.CurrentServerRole == ServerRole.Replica)
            {
                _logger.LogInformation("This is a replicate server in a load balanced setup - uSync will not run");
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
                    if (_uSyncConfig.Settings.ExportAtStartup || (_uSyncConfig.Settings.ExportOnSave && !_syncFileService.RootExists(_uSyncConfig.Settings.RootFolder)))
                    {
                        _logger.LogInformation("uSync: Running export at startup");
                        _uSyncService.Export(_uSyncConfig.Settings.RootFolder, default(SyncHandlerOptions));
                    }

                    if (IsImportAtStatupEnabled())
                    {
                        _logger.LogInformation("uSync: Running Import at startup {group}", _uSyncConfig.Settings.ImportAtStartup);

                        if (!HasStopFile(_uSyncConfig.Settings.RootFolder))
                        {
                            _uSyncService.Import(_uSyncConfig.Settings.RootFolder, false, new SyncHandlerOptions
                            {
                                Group = _uSyncConfig.Settings.ImportAtStartup
                            });

                            ProcessOnceFile(_uSyncConfig.Settings.RootFolder);
                        }
                        else
                        {
                            _logger.LogInformation("Startup Import blocked by usync.stop file");
                        }
                    }

                    if (_uSyncConfig.Settings.ExportOnSave)
                    {
                        // This is not done here any more - notification handlers are always setup, and 
                        // when they fire we check to see if ExportOnSave is set then.
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "uSync: Error duting startup {message}", ex.Message);
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
        ///  Process the once file (if it exsits we rename it to usync.stop).
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

        private bool IsImportAtStatupEnabled()
            => !string.IsNullOrWhiteSpace(_uSyncConfig.Settings.ImportAtStartup)
                && !_uSyncConfig.Settings.ImportAtStartup.InvariantEquals("none");

    }
}
