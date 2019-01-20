using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Components;
using Umbraco.Core.Logging;
using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice
{
    public class uSyncBackofficeComponent : IComponent
    {
        private readonly IProfilingLogger logger;
        private readonly SyncHandlerCollection syncHandlers;

        public uSyncBackofficeComponent(
            SyncHandlerCollection syncHandlers,
            IProfilingLogger logger)
        {
            this.syncHandlers = syncHandlers;
            this.logger = logger;
        }

        public void Initialize()
        {
            foreach(var syncHandler in syncHandlers)
            {
                logger.Debug<uSyncBackofficeComponent>($"Starting up Handler {syncHandler.Name}");
                syncHandler.InitializeEvents();
            }
        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }
    }
}
