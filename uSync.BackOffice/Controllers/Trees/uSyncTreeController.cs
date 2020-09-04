
using System.Net.Http.Formatting;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web.BackOffice.Trees;
using Umbraco.Web.Common.Attributes;
using Umbraco.Web.Common.ModelBinders;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Trees;
using Umbraco.Web.WebApi;

namespace uSync.BackOffice.Controllers.Trees
{
    [Tree(Constants.Applications.Settings, uSync.Trees.uSync,
        TreeGroup = uSync.Trees.Group,
        TreeTitle = uSync.Name, SortOrder = 35)]
    [PluginController(uSync.Name)]
    public class uSyncTreeController : TreeController
    {
        public uSyncTreeController(
            ILocalizedTextService localizedTextService, 
            UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection) 
            : base(localizedTextService, umbracoApiControllerTypeCollection)
        { }

        protected override TreeNode CreateRootNode(FormCollection queryStrings)
        {
            var root = base.CreateRootNode(queryStrings);

            root.RoutePath = $"{Constants.Applications.Settings}/{uSync.Trees.uSync}/dashboard";
            root.Icon = "icon-infinity";
            root.HasChildren = false;
            root.MenuUrl = null;

            return root;
        }

        protected override MenuItemCollection GetMenuForNode(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            return null;
        }

        protected override TreeNodeCollection GetTreeNodes(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            return new TreeNodeCollection();
        }
    }
}
