using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice.SyncHandlers;
using uSync.Core;

namespace uSync.BackOffice;

/// <summary>
///  An item notification object. 
/// </summary>
public class uSyncItemNotification<TObject> : INotification
{
    /// <inheritdoc/>
    public uSyncItemNotification(TObject? item)
    {
        this.Item = item;
    }
    /// <inheritdoc/>
    public uSyncItemNotification(TObject? item, ChangeType change)
        : this(item)
    {
        this.Change = change;
    }

    /// <inheritdoc/>
    public uSyncItemNotification(TObject? item, ISyncHandler handler)
        : this(item)
    {
        this.Handler = handler;
    }

    /// <summary>
    ///  The type of Change being notified 
    /// </summary>
    public ChangeType Change { get; set; } = ChangeType.NoChange;

    /// <summary>
    ///  The item the notification is for
    /// </summary>
    public TObject? Item { get; set; }

    /// <summary>
    ///  The handler performing the notification
    /// </summary>
    public ISyncHandler? Handler { get; set; }
}
