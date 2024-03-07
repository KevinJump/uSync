using System.Xml.Linq;

namespace uSync.BackOffice;

/// <summary>
///  Notification object for when an item is about to be reported on
/// </summary>
public class uSyncReportingItemNotification : CancelableuSyncItemNotification<XElement>
{
    /// <inheritdoc/>
    public uSyncReportingItemNotification(XElement item)
        : base(item) { }
}
