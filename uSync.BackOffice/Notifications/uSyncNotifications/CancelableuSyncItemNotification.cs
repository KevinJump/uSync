using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice.SyncHandlers.Interfaces;

namespace uSync.BackOffice;

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

    /// <summary>
    ///  Why the process was cancelled, shown to the user against the item.
    /// </summary>
    /// <remarks>
    ///  optional - if you cancel without setting this, uSync reports its generic
    ///  "change stopped by delegate event" message.
    /// </remarks>
    public string? Message { get; set; }
}
