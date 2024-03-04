using System.Collections.Generic;

using Umbraco.Cms.Core.Notifications;

namespace uSync.BackOffice;

/// <summary>
///  Notifications of bulk (starting/completed) events 
/// </summary>
public class uSyncBulkNotification : INotification
{
    /// <summary>
    ///  generate new BulkNotificationObject
    /// </summary>
    /// <param name="actions"></param>
    public uSyncBulkNotification(IEnumerable<uSyncAction> actions)
    {
        this.Actions = actions;
    }

    /// <summary>
    ///  actions that have occured during the bulk operation
    /// </summary>
    public IEnumerable<uSyncAction> Actions { get; set; }
}
