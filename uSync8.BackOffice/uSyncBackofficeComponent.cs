using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Components;
using Umbraco.Core.Logging;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice
{
    public class uSyncBackofficeComponent : IComponent
    {
        private readonly IProfilingLogger logger;
        private readonly SyncHandlerCollection syncHandlers;

        private readonly SyncFileService syncFileService;
        private readonly uSyncBackOfficeSettings globalSettings;
        private readonly uSyncService uSyncService;

        public uSyncBackofficeComponent(
            SyncHandlerCollection syncHandlers,
            IProfilingLogger logger,
            SyncFileService fileService,
            uSyncBackOfficeSettings settings,
            uSyncService uSyncService)
        {
            this.syncHandlers = syncHandlers;
            this.logger = logger;

            this.syncFileService = fileService;
            this.globalSettings = settings;
            this.uSyncService = uSyncService;
        }

        public void Initialize()
        {

            using (logger.DebugDuration<uSyncBackOfficeComposer>("uSync Starting"))
            {
                InitSettings();

                InitBackOffice();
            }
        }

        private void InitSettings()
        {
            globalSettings.LoadSettings(syncHandlers);
            foreach(var syncHandler in globalSettings.Handlers)
            {
                syncHandler.Handler.DefaultConfig = syncHandler.Config;
            }

        }

        private void InitBackOffice()
        {
            if (globalSettings.ExportAtStartup || (globalSettings.ExportOnSave && !syncFileService.RootExists(globalSettings.rootFolder)))
            {
                uSyncService.Export(globalSettings.rootFolder);
            }

            if (globalSettings.ImportAtStartup)
            {
                uSyncService.Import(globalSettings.rootFolder,false);
            }

            if (globalSettings.ExportOnSave)
            {
                foreach (var syncHandler in globalSettings.Handlers.Where(x => x.Config.Enabled))
                {
                    logger.Debug<uSyncBackofficeComponent>($"Starting up Handler {syncHandler.Handler.Name}");
                    syncHandler.Handler.Initialize();
                }
            }

        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }
    }
}
