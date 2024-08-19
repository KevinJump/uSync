using Umbraco.Cms.Core;

using uSync.Core.Dependency;

namespace uSync.Core.Sync;

/// <summary>
///  the base for a syncing item. 
/// </summary>

public class SyncEntity
{
    /// <summary>
    ///  Name (to display) of the item
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///  optional icon to display.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    ///  Umbraco UDI value to identify the item.
    /// </summary>
    public required Udi Udi { get; set; }

    /// <summary>
    ///  the entity type for the item.
    /// </summary>
    public string EntityType => Udi.EntityType;
}

/// <summary>
///  An item involved in a sync between servers. SyncItems are the start of any sync process
/// </summary>
public class SyncItem : SyncEntity
{
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
