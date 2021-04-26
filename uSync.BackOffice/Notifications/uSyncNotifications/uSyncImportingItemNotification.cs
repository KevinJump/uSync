using System.Xml.Linq;

namespace uSync.BackOffice
{
    public class uSyncImportingItemNotification : CancelableuSyncItemNotification<XElement> { 
        public uSyncImportingItemNotification(XElement item) 
            : base(item) { }
    }


}
