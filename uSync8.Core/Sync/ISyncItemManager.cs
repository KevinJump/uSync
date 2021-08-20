
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.Core.Sync
{
    /// <summary>
    ///  item managers all us to intergrate something into uSyncComplete's menus and 
    ///  processes. 
    /// </summary>
    public interface ISyncItemManager
    {
        /// <summary>yc
        ///  Can these items be exported in uSync Exporter
        /// </summary>
        /// <remarks>
        ///  Unless the item has some form of shared picker tree with the
        ///  core umbraco UI then answer is likely no (for now)
        /// </remarks>
        bool CanExport { get; }

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
        ///  Get all the decendents of the provided item (based on flags)
        /// </summary>
        /// <remarks>
        ///  These items form the basis of a sync - from these core items 
        ///  everything else will be calculated. 
        /// </remarks>
        IEnumerable<SyncItem> GetDecendants(SyncItem item);

        /// <summary>
        ///  Get the underling Local item for something that was picked from the tree.
        /// </summary>
        SyncLocalItem GetEntity(SyncTreeItem treeItem);
    }

}
