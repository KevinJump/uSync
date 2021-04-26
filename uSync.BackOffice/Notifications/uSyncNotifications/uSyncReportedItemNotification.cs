using System.Xml.Linq;

using uSync.Core;

namespace uSync.BackOffice
{
    public class uSyncReportedItemNotification : uSyncItemNotification<XElement> {
        public uSyncReportedItemNotification(XElement item, ChangeType change) 
            : base(item, change) { }
    }


}
