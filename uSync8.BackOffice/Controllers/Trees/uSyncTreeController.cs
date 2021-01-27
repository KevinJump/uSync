using System.Net.Http.Formatting;
using System.Web.Http.ModelBinding;

using Umbraco.Core;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Mvc;
using Umbraco.Web.Trees;
using Umbraco.Web.WebApi.Filters;

namespace uSync8.BackOffice.Controllers.Trees
{
    [Tree(Constants.Applications.Settings, uSync.Trees.uSync,
        TreeGroup = uSync.Trees.Group,
        TreeTitle = uSync.Name, SortOrder = 35)]
    [PluginController(uSync.Name)]
    public class uSyncTreeController : TreeController
    {
        protected override TreeNode CreateRootNode(FormDataCollection queryStrings)
        {
            var root = base.CreateRootNode(queryStrings);

            root.RoutePath = $"{this.SectionAlias}/{uSync.Trees.uSync}/dashboard";
            root.Icon = "icon-infinity";
            root.HasChildren = false;
            root.MenuUrl = null;

            return root;
        }

        protected override MenuItemCollection GetMenuForNode(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormDataCollection queryStrings)
        {
            return null;
        }

        protected override TreeNodeCollection GetTreeNodes(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormDataCollection queryStrings)
        {
            return new TreeNodeCollection();
        }
    }
}
