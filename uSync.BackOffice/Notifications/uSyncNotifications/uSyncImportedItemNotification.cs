using System.Xml.Linq;

using uSync.Core;

namespace uSync.BackOffice
{
    public class uSyncImportedItemNotification : uSyncItemNotification<XElement> { 
        public uSyncImportedItemNotification(XElement item, ChangeType change)
            : base(item, change) { }
    }


}
