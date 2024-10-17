using System.Xml.Linq;

using uSync.BackOffice.SyncHandlers.Interfaces;

namespace uSync.BackOffice;

/// <summary>
///  Cancelable nofiration called before an item is imported 
/// </summary>
public class uSyncImportingItemNotification : CancelableuSyncItemNotification<XElement>
{
    /// <summary>
    ///  generate a new uSyncImportingItemNotification object
    /// </summary>
    public uSyncImportingItemNotification(XElement item)
        : base(item) { }

    /// <summary>
    ///  generate a new uSyncImportingItemNotification object
    /// </summary>
    public uSyncImportingItemNotification(XElement item, ISyncHandler handler)
        : base(item, handler) { }

}
