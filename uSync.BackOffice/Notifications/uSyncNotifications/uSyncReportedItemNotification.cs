using System.Xml.Linq;

using uSync.Core;

namespace uSync.BackOffice
{
    /// <summary>
    /// Notification object when an item has been reported
    /// </summary>
    public class uSyncReportedItemNotification : uSyncItemNotification<XElement> 
    {
        /// <inheritdoc/>
        public uSyncReportedItemNotification(XElement item, ChangeType change) 
            : base(item, change) { }
    }
}
