using System.Xml.Linq;

using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice
{
    public class uSyncImportingItemNotification : CancelableuSyncItemNotification<XElement> {
        public uSyncImportingItemNotification(XElement item)
            : base(item) { }

        public uSyncImportingItemNotification(XElement item, ISyncHandler handler)
            : base(item, handler) { }

    }


}
