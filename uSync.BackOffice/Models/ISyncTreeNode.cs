using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Trees;
using Umbraco.Cms.Web.BackOffice.Trees;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.ModelBinders;

namespace uSync.BackOffice.Models
{
    /// <summary>
    ///  add on for uSync that allows you to render a node 
    ///  under the uSync tree. 
    /// </summary>
    public interface ISyncTreeNode
    {
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

    }

    public class uSyncTreeNode
    {
        public string Id { get; set; }
        public string Alias { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
    }
}
