using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Core.Trees;

namespace uSync.BackOffice.Expansions
{
    /// <summary>
    ///  add on for uSync that allows you to render a node 
    ///  under the uSync tree. 
    /// </summary>
    public interface ISyncTreeNode
    {
        /// <summary>
        ///  if the tree node is in-fact enabled. 
        /// </summary>
        public bool Disabled { get; }

        /// <summary>
        ///  position in the tree, (higher is lower down the tree)
        /// </summary>
        public int Weight { get; }

        /// <summary>
        ///  Id this will be passed to the controller.
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///  alias for the tree
        /// </summary>
        public string TreeAlias { get; }

        /// <summary>
        ///  alias for the node item
        /// </summary>
        public string Alias { get; }

        /// <summary>
        ///  title of the tree item
        /// </summary>
        public string Title { get; }

        /// <summary>
        ///  icon for the tree item. 
        /// </summary>
        public string Icon { get; }


        /// <summary>
        ///  method to return any additional child nodes under the parent node
        /// </summary>
        public IEnumerable<uSyncTreeNode> GetChildNodes(string id, FormCollection queryStrings);

        /// <summary>
        ///  to display any context menu.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="queryStrings"></param>
        /// <returns></returns>
        public ActionResult<MenuItemCollection> GetMenuItems(string id, FormCollection queryStrings);
    }

    /// <summary>
    ///  Representation of a single tree node
    /// </summary>
    public class uSyncTreeNode
    {
        /// <summary>
        ///  Id for this tree node 
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///  Alias of the tree item
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        ///  title (shown to user) for tree item
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///  Icon to display. 
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        ///  segment path to this item. 
        /// </summary>
        public string Path { get; set; }
    }
}
