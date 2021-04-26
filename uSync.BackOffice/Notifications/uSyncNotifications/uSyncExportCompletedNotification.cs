using System.Collections.Generic;

namespace uSync.BackOffice
{
    public class uSyncExportCompletedNotification : uSyncBulkNotification { 
        public uSyncExportCompletedNotification(IEnumerable<uSyncAction> actions)
            : base(actions) { }
    }


}
