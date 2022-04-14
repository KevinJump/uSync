using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice
{
    /// <summary>
    ///  Cancelable uSync event 
    /// </summary>
    public class CancelableuSyncItemNotification<TObject> : uSyncItemNotification<TObject>, ICancelableNotification
    {
        /// <summary>
        /// Construct a new cancelable event of type item
        /// </summary>
        public CancelableuSyncItemNotification(TObject item)
            : base(item)
        { }

        /// <summary>
        /// Construct a new cancelable event of type item for a specific handler 
        /// </summary>
        public CancelableuSyncItemNotification(TObject item, ISyncHandler handler)
            : base(item, handler)
        { }

        /// <summary>
        ///  Cancel the current process
        /// </summary>
        public bool Cancel { get; set; }
    }


}
