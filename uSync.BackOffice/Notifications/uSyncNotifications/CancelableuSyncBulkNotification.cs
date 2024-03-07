using System.Linq;

using Umbraco.Cms.Core.Notifications;

namespace uSync.BackOffice;

/// <summary>
///  uSync cancelable bulk notification event (fired for "starting" events)
/// </summary>
public class CancelableuSyncBulkNotification : uSyncBulkNotification, ICancelableNotification
{
    /// <summary>
    ///  Notification constructor
    /// </summary>
    public CancelableuSyncBulkNotification()
        : base(Enumerable.Empty<uSyncAction>())
    { }

    /// <summary>
    ///  Cancel this process 
    /// </summary>
    /// <remarks>
    ///  if this value is set to true then uSync will cancel the process it is currently running
    /// </remarks>
    public bool Cancel { get; set; }
}
