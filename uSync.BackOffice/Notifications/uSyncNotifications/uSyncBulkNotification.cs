using System.Collections.Generic;

using Umbraco.Cms.Core.Events;

namespace uSync.BackOffice
{
    public class uSyncBulkNotification : INotification
    {
        public uSyncBulkNotification(IEnumerable<uSyncAction> actions)
        {
            this.Actions = actions;
        }

        public IEnumerable<uSyncAction> Actions { get; set; }
    }


}
