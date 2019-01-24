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

        public uSyncBackofficeComponent(
            SyncHandlerCollection syncHandlers,
            IProfilingLogger logger,
            SyncFileService fileService,
            uSyncBackOfficeSettings settings)
        {
            this.syncHandlers = syncHandlers;
            this.logger = logger;

            this.syncFileService = fileService;
            this.globalSettings = settings;
        }

        public void Initialize()
        {

            using (logger.DebugDuration<uSyncBackOfficeComposer>("uSync Starting"))
            {
                InitBackOffice();
            }
        }

        private void InitBackOffice()
        {
            if (globalSettings.ExportAtStartup || (globalSettings.ExportOnSave && !syncFileService.RootExists()))
            {
                foreach (var syncHandler in syncHandlers.Where(x => x.Enabled))
                {
                    using (logger.DebugDuration<uSyncBackofficeComponent>($"Exporting: {syncHandler.Name}"))
                    {
                        syncHandler.ExportAll(syncHandler.DefaultFolder);
                    }
                }
            }

            if (globalSettings.ImportAtStartup)
            {
                foreach (var syncHandler in syncHandlers.Where(x => x.Enabled))
                {
                    using (logger.DebugDuration<uSyncBackofficeComponent>($"Importing: {syncHandler.Name}"))
                    {
                        syncHandler.ImportAll(syncHandler.DefaultFolder, false);
                    }
                }
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
