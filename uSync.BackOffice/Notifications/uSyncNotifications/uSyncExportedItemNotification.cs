using System.Xml.Linq;

using uSync.Core;

namespace uSync.BackOffice
{
    /// <summary>
    ///  Notification object created after an item has been exported
    /// </summary>
    public class uSyncExportedItemNotification : uSyncItemNotification<XElement> {
        /// <inheritdoc/>
        public uSyncExportedItemNotification(XElement item, ChangeType change)
            : base(item, change) { }
    }


}
