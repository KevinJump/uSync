using System;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice
{
    public class uSyncBackofficeComponent : IComponent
    {
        private readonly IProfilingLogger logger;
        private readonly SyncHandlerCollection syncHandlers;

        private readonly SyncFileService syncFileService;
        private readonly uSyncSettings globalSettings;
        private readonly uSyncService uSyncService;
        private readonly IRuntimeState runtimeState;

        public uSyncBackofficeComponent(
            SyncHandlerCollection syncHandlers,
            IProfilingLogger logger,
            SyncFileService fileService,            
            uSyncService uSyncService,
            IRuntimeState runtimeState)
        {
            globalSettings = Current.Configs.uSync();

            this.runtimeState = runtimeState;
            this.syncHandlers = syncHandlers;
            this.logger = logger;

            this.syncFileService = fileService;
            this.uSyncService = uSyncService;

        }

        public void Initialize()
        {
            if (runtimeState.Level <= RuntimeLevel.Run)
            {
                logger.Info<uSyncBackofficeComponent>("Umbraco is not in Run Mode {0} so uSync is not going to run", runtimeState.Level);
                return;
            }

            if (runtimeState.ServerRole == Umbraco.Core.Sync.ServerRole.Replica)
            {
                logger.Info<uSyncBackofficeComponent>("This is a replica server, uSync will not run any of the startup events");
                return;
            }

            using (logger.DebugDuration<uSyncBackofficeComponent>("uSync Starting"))
            {
                InitBackOffice();
            }
        }

        private void InitBackOffice()
        {
            if (globalSettings.ExportAtStartup || (globalSettings.ExportOnSave && !syncFileService.RootExists(globalSettings.RootFolder)))
            {
                uSyncService.Export(globalSettings.RootFolder);
            }

            if (globalSettings.ImportAtStartup)
            {
                uSyncService.Import(globalSettings.RootFolder,false);
            }

            if (globalSettings.ExportOnSave)
            {
                var handlers = syncHandlers.GetValidHandlers("Save", globalSettings);
                foreach (var syncHandler in handlers)
                {
                    logger.Debug<uSyncBackofficeComponent>($"Starting up Handler {syncHandler.Handler.Name}");
                    syncHandler.Handler.Initialize(syncHandler.Settings);
                }
            }

        }

        public void Terminate()
        {
            logger.Debug<uSyncBackofficeComponent>("Terminiating Component");
        }
    }
}
