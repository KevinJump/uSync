using System;
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
    public uSyncBulkNotification(IEnumerable<uSyncAction> actions, string? group)
    {
        this.Group = group;
        this.Actions = actions;
    }

    /// <summary>
    ///  The group e.g settings, content that this action ran under.
    /// </summary>
    public string? Group { get; set; }   

    /// <summary>
    ///  actions that have occured during the bulk operation
    /// </summary>
    public IEnumerable<uSyncAction> Actions { get; set; }
}
