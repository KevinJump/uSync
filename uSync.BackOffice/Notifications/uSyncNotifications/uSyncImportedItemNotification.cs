using System.Xml.Linq;

using uSync.Core;

namespace uSync.BackOffice
{
    /// <summary>
    ///  Notification fired when a single item has been imported
    /// </summary>
    public class uSyncImportedItemNotification : uSyncItemNotification<XElement> 
    {
        /// <inheritdoc/>
        public uSyncImportedItemNotification(XElement item, ChangeType change)
            : base(item, change) { }
    }


}
