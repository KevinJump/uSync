using System.Collections.Generic;

namespace uSync.BackOffice
{
    /// <summary>
    ///  bulk notification fired when import process is completed
    /// </summary>
    public class uSyncImportCompletedNotification : uSyncBulkNotification 
    {
        /// <inheritdoc/>
        public uSyncImportCompletedNotification(IEnumerable<uSyncAction> actions)
            : base(actions) { }
    }


}
