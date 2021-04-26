
using Umbraco.Cms.Core.Events;

namespace uSync.BackOffice
{
    public class CancelableuSyncItemNotification<TObject> : uSyncItemNotification<TObject>, ICancelableNotification
    {
        public CancelableuSyncItemNotification(TObject item)
            : base(item)
        { }

        public bool Cancel { get; set; }
    }


}
