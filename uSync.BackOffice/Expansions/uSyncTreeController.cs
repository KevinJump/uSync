using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Trees;
using Umbraco.Cms.Web.BackOffice.Trees;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.ModelBinders;
using Umbraco.Extensions;
using uSync.BackOffice.Models;

namespace uSync.BackOffice.Expansions
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
        public SyncTreeNodeCollection _treeNodes;
        private readonly IMenuItemCollectionFactory _menuItemsFactory;

        /// <inheritdoc/>
        public uSyncTreeController(
            ILocalizedTextService localizedTextService,
            UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection,
            IEventAggregator eventAggregator,
            SyncTreeNodeCollection treeNodes,
            IMenuItemCollectionFactory menuItemsFactory)
            : base(localizedTextService, umbracoApiControllerTypeCollection, eventAggregator)
        {
            _treeNodes = treeNodes;
            _menuItemsFactory = menuItemsFactory;
        }

        /// <inheritdoc/>
        protected override ActionResult<TreeNode> CreateRootNode(FormCollection queryStrings)
        {
            var result = base.CreateRootNode(queryStrings);

            result.Value.RoutePath = $"{SectionAlias}/{uSync.Trees.uSync}/dashboard";
            result.Value.Icon = "icon-infinity";
            result.Value.HasChildren = _treeNodes.Count > 0;
            result.Value.MenuUrl = null;

            return result.Value;
        }

        private string getParentId(string id) 
            => id.IndexOf('_') < 0 ? id : id.Substring(0, id.IndexOf("_"));

        /// <inheritdoc/>
        protected override ActionResult<MenuItemCollection> GetMenuForNode(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            var defaultMenu = _menuItemsFactory.Create();

            if (_treeNodes.Count == 0) return defaultMenu;
            if (id == Constants.System.RootString) return defaultMenu;

            var parentId = getParentId(id);
            var current = _treeNodes.FirstOrDefault(x => x.Id == parentId);
            return current?.GetMenuItems(id, queryStrings) ?? defaultMenu;
        }

        /// <inheritdoc/>
        protected override ActionResult<TreeNodeCollection> GetTreeNodes(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            if (_treeNodes.Count == 0) return new TreeNodeCollection();

            var collection = new TreeNodeCollection();

            if (id == Constants.System.RootString)
            {
                foreach (var node in _treeNodes.OrderBy(x => x.Weight))
                {
                    var treeNode = CreateTreeNode(
                        node.Id,
                        id,
                        queryStrings,
                        node.Title,
                        node.Icon,
                        $"{SectionAlias}/{node.TreeAlias}/{node.Alias}");

                    var children = node.GetChildNodes(id, queryStrings);
                    if (children?.Any() == true)
                        treeNode.HasChildren = true;

                    collection.Add(treeNode);

                }

                return collection;
            }
            else
            {
                var treeNode = _treeNodes.FirstOrDefault(x => x.Id == getParentId(id));
                if (treeNode != null)
                {
                    var children = treeNode.GetChildNodes(id, queryStrings);
                    if (children != null)
                    {
                        foreach (var child in children)
                        {
                            collection.Add(CreateTreeNode(
                                $"{id}_{child.Id}",
                                id,
                                queryStrings,
                                child.Title,
                                child.Icon,
                                $"{SectionAlias}/{treeNode.TreeAlias}/{child.Path}"));
                        }
                    }
                }
            }

            return collection;

        }
    }
}
