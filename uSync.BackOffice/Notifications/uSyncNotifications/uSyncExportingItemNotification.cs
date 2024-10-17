using uSync.BackOffice.SyncHandlers.Interfaces;

namespace uSync.BackOffice;

/// <summary>
///  Cancelable notification called before an item is Exported 
/// </summary>
public class uSyncExportingItemNotification<TObject> : CancelableuSyncItemNotification<TObject>
{
    /// <summary>
    /// generate a new uSyncExportingItemNotification object
    /// </summary>
    public uSyncExportingItemNotification(TObject item)
        : base(item) { }

    /// <summary>
    /// generate a new uSyncExportingItemNotification object
    /// </summary>
    public uSyncExportingItemNotification(TObject item, ISyncHandler syncHandler)
        : base(item, syncHandler) { }
}
