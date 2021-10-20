using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice
{
    public class CancelableuSyncItemNotification<TObject> : uSyncItemNotification<TObject>, ICancelableNotification
    {
        public CancelableuSyncItemNotification(TObject item)
            : base(item)
        { }

        public CancelableuSyncItemNotification(TObject item, ISyncHandler handler)
            : base(item, handler)
        { }

        public bool Cancel { get; set; }
    }


}
