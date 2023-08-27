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
        public readonly ITypeFinder _typeFinder;

        public IList<ISyncTreeNode> _treeNodes;

        /// <inheritdoc/>
        public uSyncTreeController(
            ILocalizedTextService localizedTextService,
            UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection,
            IEventAggregator eventAggregator,
            ITypeFinder typeFinder)
            : base(localizedTextService, umbracoApiControllerTypeCollection, eventAggregator)
        {
            _typeFinder = typeFinder;
            _treeNodes = GetTreeNodes();
        }

        /// <inheritdoc/>
        protected override ActionResult<TreeNode> CreateRootNode(FormCollection queryStrings)
        {
            var result = base.CreateRootNode(queryStrings);

            result.Value.RoutePath = $"{this.SectionAlias}/{uSync.Trees.uSync}/dashboard";
            result.Value.Icon = "icon-infinity";
            result.Value.HasChildren = _treeNodes.Count > 0;
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
            if (_treeNodes.Count == 0) return new TreeNodeCollection();

            var collection = new TreeNodeCollection();

            if (id == "-1")
            {
                foreach (var node in _treeNodes.OrderBy(x => x.Weight))
                {
                    var treeNode = CreateTreeNode(
                        node.Id,
                        id,
                        queryStrings,
                        node.Title,
                        node.Icon,
                        $"{this.SectionAlias}/{node.TreeAlias}/{node.Alias}");

                    var children = node.GetChildNodes(id, queryStrings);
                    if (children != null)
                        treeNode.HasChildren = true;

                    collection.Add(treeNode);

                }

                return collection;
            }
            else
            {
                var treeNode = _treeNodes.FirstOrDefault(x => x.Id == id);
                if (treeNode != null)
                {
                    var children = treeNode.GetChildNodes(id, queryStrings);
                    if (children != null)
                    {
                        foreach(var child in children)
                        {
                            collection.Add(CreateTreeNode(
                                child.Id, 
                                id, 
                                queryStrings, 
                                child.Title,
                                child.Icon,
                                $"{this.SectionAlias}/{treeNode.TreeAlias}/{child.Alias}"));
                        }
                    }
                }
            }

            return collection;

        }

        private IList<ISyncTreeNode> GetTreeNodes()
        {
            var treeNodes = new List<ISyncTreeNode>();
            var nodes = _typeFinder.FindClassesOfType<ISyncTreeNode>();
            foreach(var node in nodes)
            {
                if (Activator.CreateInstance(node) is ISyncTreeNode instance)
                {
                    treeNodes.Add(instance);
                }
            }

            return treeNodes;
        }
    }
}
