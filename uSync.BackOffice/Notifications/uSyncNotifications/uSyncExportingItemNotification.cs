namespace uSync.BackOffice
{
    public class uSyncExportingItemNotification<TObject> : CancelableuSyncItemNotification<TObject> {
        public uSyncExportingItemNotification(TObject item)
            : base(item) { }
    }


}
