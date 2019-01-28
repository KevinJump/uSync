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
            globalSettings.LoadSettings(syncHandlers);

            using (logger.DebugDuration<uSyncBackOfficeComposer>("uSync Starting"))
            {
                InitBackOffice();
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
                foreach (var syncHandler in syncHandlers.Where(x => x.Enabled))
                {
                    logger.Debug<uSyncBackofficeComponent>($"Starting up Handler {syncHandler.Name}");
                    syncHandler.InitializeEvents();
                }
            }

        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }
    }
}
