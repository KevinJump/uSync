using System;
using System.Threading.Tasks;

namespace uSync.BackOffice.Models;

/// <summary>
///  options for building an item preview.
/// </summary>
public class SyncPreviewOptions
{
    /// <summary>
    ///  finds items that are in the uSync files but not in Umbraco yet (item type, key),
    ///  so the previewed item can use things the import would create first.
    /// </summary>
    public Func<Type, Guid, Task<object?>>? PendingItemResolver { get; set; }
}
