using System.Xml.Linq;

using uSync.Core;

namespace uSync.BackOffice
{
    public class uSyncExportedItemNotification : uSyncItemNotification<XElement> {
        public uSyncExportedItemNotification(XElement item, ChangeType change)
            : base(item, change) { }
    }


}
