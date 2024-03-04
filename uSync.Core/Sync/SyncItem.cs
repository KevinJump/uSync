using Umbraco.Cms.Core;

using uSync.Core.Dependency;

namespace uSync.Core.Sync;

/// <summary>
///  An item involved in a sync between servers. SyncItems are the start of any sync process
/// </summary>
public class SyncItem
{
    /// <summary>
    ///  Name (to display) of the item
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///  Umbraco UDI value to identify the item.
    /// </summary>
    public Udi? Udi { get; set; }

    /// <summary>
    ///  Flags controlling what is to be included when this item is exported
    /// </summary>
    public DependencyFlags Flags { get; set; } = DependencyFlags.None;

    /// <summary>
    ///  Type of change to be performed (reserved)
    /// </summary>
    public ChangeType Change { get; set; }

    public SyncItem() { }

    public SyncItem(DependencyFlags flags)
    {
        Flags = flags;
    }
}
