using System.Collections.Generic;

namespace uSync.BackOffice
{
    public class uSyncImportCompletedNotification : uSyncBulkNotification {
        public uSyncImportCompletedNotification(IEnumerable<uSyncAction> actions)
            : base(actions) { }
    }


}
