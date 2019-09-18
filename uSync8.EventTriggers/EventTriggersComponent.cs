using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using uSync8.BackOffice;

namespace uSync8.EventTriggers
{
    public class EventTriggersComposer : ComponentComposer<EventTriggersComponent> { }

    /// <summary>
    ///  testing the even triggers fire when we expect them to. 
    /// </summary>
    public class EventTriggersComponent : IComponent
    {
        private readonly IProfilingLogger logger;

        public EventTriggersComponent(IProfilingLogger logger)
        {
            this.logger = logger;
        }

        public void Initialize()
        {
            uSyncService.ImportStarting += BulkEventStarting;
            uSyncService.ImportComplete += BulkEventComplete;

            uSyncService.ExportStarting += BulkEventStarting;
            uSyncService.ExportComplete += BulkEventComplete;

            uSyncService.ReportStarting += BulkEventStarting;
            uSyncService.ReportComplete += BulkEventComplete;
        }

        private void BulkEventStarting(uSyncBulkEventArgs e)
        {
            logger.Info<EventTriggersComponent>("BulkEvent Start Triggered");
        }

        private void BulkEventComplete(uSyncBulkEventArgs e)
        {
            logger.Info<EventTriggersComponent>("BulkEvent Complete Triggered");
        }

        public void Terminate()
        {
            // end
        }
    }
}
