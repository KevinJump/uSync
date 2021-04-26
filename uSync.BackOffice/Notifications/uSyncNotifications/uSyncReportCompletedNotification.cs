using System.Collections.Generic;

namespace uSync.BackOffice
{
    public class uSyncReportCompletedNotification : uSyncBulkNotification { 
        public uSyncReportCompletedNotification(IEnumerable<uSyncAction> actions) 
            : base(actions) { }
    }


}
