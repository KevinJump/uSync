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

    /// <summary>
    ///  is this a forced import (the user has asked for the item to be imported
    ///  regardless of whether anything appears to have changed).
    /// </summary>
    /// <remarks>
    ///  anything short-cutting the import because it believes nothing has changed
    ///  should stand down when this is set - a forced import is how someone gets a
    ///  guaranteed, full import.
    /// </remarks>
    public bool Force { get; set; }
}
