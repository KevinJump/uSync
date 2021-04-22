using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Web;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Notifications
{
    public class uSyncApplicationStartingHandler : INotificationHandler<UmbracoApplicationStarting>
    {
        private ILogger<uSyncApplicationStartingHandler> logger;

        private IRuntimeState runtimeState;
        private IServerRoleAccessor serverRegistrar;

        private IUmbracoContextFactory umbracoContextFactory;

        private readonly uSyncConfigService uSyncConfig;

        private SyncFileService syncFileService;
        private uSyncService uSyncService;
        private SyncHandlerFactory handlerFactory;

        public uSyncApplicationStartingHandler(
            ILogger<uSyncApplicationStartingHandler> logger,
            IRuntimeState runtimeState,
            IServerRoleAccessor serverRegistrar,
            IUmbracoContextFactory umbracoContextFactory,
            uSyncConfigService uSyncConfigService,
            SyncFileService syncFileService,
            uSyncService uSyncService,
            SyncHandlerFactory handlerFactory)
        {
            this.runtimeState = runtimeState;
            this.serverRegistrar = serverRegistrar;

            this.umbracoContextFactory = umbracoContextFactory;

            this.logger = logger;

            this.uSyncConfig = uSyncConfigService;

            this.syncFileService = syncFileService;
            this.uSyncService = uSyncService;
            this.handlerFactory = handlerFactory;
        }

        public void Handle(UmbracoApplicationStarting notification)
        {
            if (runtimeState.Level < RuntimeLevel.Run)
            {
                logger.LogInformation("Umbaco is not in Run mode, so uSync will not run");
                return;
            }

            if (serverRegistrar.CurrentServerRole == ServerRole.Replica)
            {
                logger.LogInformation("This is a replicate server in a load balanced setup - uSync will not run");
                return;
            }

            InituSync();
        }

        private void InituSync()
        {
            var sw = Stopwatch.StartNew();

            try
            {
                using (var reference = umbracoContextFactory.EnsureUmbracoContext())
                {
                    if (uSyncConfig.Settings.ExportAtStartup || (uSyncConfig.Settings.ExportOnSave && !syncFileService.RootExists(uSyncConfig.Settings.RootFolder)))
                    {
                        logger.LogInformation("uSync: Running export at startup");
                        uSyncService.Export(uSyncConfig.Settings.RootFolder, default(SyncHandlerOptions));
                    }

                    if (uSyncConfig.Settings.ImportAtStartup)
                    {
                        logger.LogInformation("uSync: Running Import at startup");

                        if (!HasStopFile(uSyncConfig.Settings.RootFolder))
                        {
                            uSyncService.Import(uSyncConfig.Settings.RootFolder, false, new SyncHandlerOptions
                            {
                                Group = uSyncConfig.Settings.ImportAtStartupGroup
                            });

                            ProcessOnceFile(uSyncConfig.Settings.RootFolder);
                        }
                        else
                        {
                            logger.LogInformation("Startup Import blocked by usync.stop file");
                        }
                    }

                    if (uSyncConfig.Settings.ExportOnSave)
                    {
                        // This is not done here any more - notification handlers are always setup, and 
                        // when they fire we check to see if ExportOnSave is set then.
                    }
                }
            }
            catch(Exception ex)
            {
                logger.LogWarning(ex, "uSyc: Error duting startup {message}", ex.Message);
            }
            finally
            {
                sw.Stop();
                logger.LogInformation("uSync: Startup Complete {elapsed}ms", sw.ElapsedMilliseconds);
            }

        }

        private bool HasStopFile(string folder)
            => syncFileService.FileExists($"{folder}/usync.stop");

        private void ProcessOnceFile(string folder)
        {
            if (syncFileService.FileExists($"{folder}/usync.once"))
            {
                syncFileService.DeleteFile($"{folder}/usync.once");
                syncFileService.SaveFile($"{folder}/usync.stop", "uSync Stop file, prevents startup import");
                logger.LogInformation("usync.once file replaced by usync.stop file");
            }
        }

    }
}
