using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Components;
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

        public uSyncBackofficeComponent(
            SyncHandlerCollection syncHandlers,
            IProfilingLogger logger,
            SyncFileService fileService,            
            uSyncService uSyncService)
        {
            globalSettings = Current.Configs.uSync();

            this.syncHandlers = syncHandlers;
            this.logger = logger;

            this.syncFileService = fileService;
            this.uSyncService = uSyncService;

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
            throw new NotImplementedException();
        }
    }
}
