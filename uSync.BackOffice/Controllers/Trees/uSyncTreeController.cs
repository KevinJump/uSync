
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Trees;
using Umbraco.Cms.Web.BackOffice.Trees;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.ModelBinders;

namespace uSync.BackOffice.Controllers.Trees
{
    /// <summary>
    ///  Tree controller for the 'uSync' tree
    /// </summary>
    [Tree(Constants.Applications.Settings, uSync.Trees.uSync,
        TreeGroup = uSync.Trees.Group,
        TreeTitle = uSync.Name, SortOrder = 35)]
    [PluginController(uSync.Name)]
    public class uSyncTreeController : TreeController
    {
        /// <inheritdoc/>
        public uSyncTreeController(
            ILocalizedTextService localizedTextService,
            UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection,
            IEventAggregator eventAggregator)
            : base(localizedTextService, umbracoApiControllerTypeCollection, eventAggregator)
        { }

        /// <inheritdoc/>
        protected override ActionResult<TreeNode> CreateRootNode(FormCollection queryStrings)
        {
            var result = base.CreateRootNode(queryStrings);

            result.Value.RoutePath = $"{this.SectionAlias}/{uSync.Trees.uSync}/dashboard";
            result.Value.Icon = "icon-infinity";
            result.Value.HasChildren = false;
            result.Value.MenuUrl = null;

            return result.Value;
        }

        /// <inheritdoc/>
        protected override ActionResult<MenuItemCollection> GetMenuForNode(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            return null;
        }

        /// <inheritdoc/>
        protected override ActionResult<TreeNodeCollection> GetTreeNodes(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            return new TreeNodeCollection();
        }
    }
}
