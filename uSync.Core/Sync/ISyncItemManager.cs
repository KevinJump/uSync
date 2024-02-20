namespace uSync.Core.Sync;

/// <summary>
///  item managers all us to intergrate something into uSyncComplete's menus and 
///  processes. 
/// </summary>
public interface ISyncItemManager
{
    /// <summary>
    ///  Return information to let people pick this for export.
    /// </summary>
    /// <remarks>
    ///  Unless the item has some form of shared picker tree with the
    ///  core umbraco UI then answer is likely no (for now)
    /// </remarks>
    SyncEntityInfo GetSyncInfo(string entityType);

    /// <summary>
    ///   Which type of tree menu should be used. 
    /// </summary>
    /// <remarks> 
    ///   for most items will be 'settings' (this is the default is base class).
    ///   
    ///   if showing the menu is based on permissions, then it 
    ///   would be content/media (depending on the tree).
    ///   
    ///   If your own tree has node based permissions
    ///   you should set this to none and impliment your 
    ///   own menu rendering logic. 
    /// </remarks>
    SyncTreeType GetTreeType(SyncTreeItem treeItem);


    /// <summary>
    ///  Entity types that this item manager can return
    /// </summary>
    string[] EntityTypes { get; }

    /// <summary>
    ///  Alias of the Tree in umbraco that this item manager can work with
    /// </summary>
    /// 
    string[] Trees { get; }

    /// <summary>
    ///  Get all items that we want to sync. 
    /// </summary>
    /// <remarks>
    ///  These items form the basis of a sync - from these core items 
    ///  everything else will be calculated. 
    ///  
    ///  the process should as a bare minimum return the item it is passed, 
    ///  when the Include children flag is set - it should also return children. 
    /// </remarks>
    IEnumerable<SyncItem> GetItems(SyncItem item);

    /// <summary>
    ///  Get the underling Local item for something that was picked from the tree.
    /// </summary>
    SyncLocalItem GetEntity(SyncTreeItem treeItem);
}
