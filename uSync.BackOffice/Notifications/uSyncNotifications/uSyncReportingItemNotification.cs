using System.Xml.Linq;

namespace uSync.BackOffice
{
    public class uSyncReportingItemNotification : CancelableuSyncItemNotification<XElement> {
        public uSyncReportingItemNotification(XElement item)
            : base(item) { }
    }


}
