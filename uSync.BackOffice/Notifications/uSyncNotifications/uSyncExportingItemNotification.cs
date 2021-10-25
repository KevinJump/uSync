using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice
{
    public class uSyncExportingItemNotification<TObject> : CancelableuSyncItemNotification<TObject> {
        public uSyncExportingItemNotification(TObject item)
            : base(item) { }

        public uSyncExportingItemNotification(TObject item, ISyncHandler syncHandler)
            : base(item, syncHandler) { }
    }


}
