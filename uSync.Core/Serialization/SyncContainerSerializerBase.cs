﻿using System.Web;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace uSync.Core.Serialization;

public abstract class SyncContainerSerializerBase<TObject>
    : SyncTreeSerializerBase<TObject>, ISyncCachedSerializer
    where TObject : ITreeEntity
{
    protected UmbracoObjectTypes containerType;

    public SyncContainerSerializerBase(IEntityService entityService, ILogger<SyncContainerSerializerBase<TObject>> logger, UmbracoObjectTypes containerType)
        : base(entityService, logger)
    {
        this.containerType = containerType;
    }

    protected override Attempt<TObject> FindOrCreate(XElement node)
    {

        TObject? item = FindItem(node);
        if (item is null) return Attempt.Succeed<TObject>(item);

        logger.LogDebug("FindOrCreate: Creating");

        // create
        var parent = default(TObject);
        var treeItem = default(ITreeEntity);

        var info = node.Element(uSyncConstants.Xml.Info);

        var parentNode = info?.Element(uSyncConstants.Xml.Parent);
        if (parentNode is not null)
        {
            logger.LogDebug("Finding Parent");

            var parentKey = parentNode.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);
            parent = FindItem(parentKey, parentNode.Value);
            if (parent != null)
            {
                logger.LogDebug("Parent Found {parentId}", parent.Id);
                treeItem = parent;
            }
        }

        if (parent is null)
        {
            // might be in a folder 
            var folder = info?.Element("Folder");
            if (folder is not null)
            {

                var folderKey = folder.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);

                logger.LogDebug("Searching for Parent by folder {folderKey} {folderValue}", folderKey, folder.Value);

                var container = FindFolder(folderKey, folder.Value);
                if (container != null)
                {
                    treeItem = container;
                    logger.LogDebug("Parent is Folder {TreeItemId}", treeItem.Id);

                    // update the container key if its different (because we don't serialize folders on their own)
                    if (container.Key != folderKey)
                    {
                        if (container.Key != folderKey)
                        {
                            logger.LogDebug("Folder Found: Key Different");
                            container.Key = folderKey;
                            SaveContainer(container);
                        }
                    }
                }
            }
        }

        if (parent is null || treeItem is null)
            return Attempt.Fail(item, new KeyNotFoundException("Unable to find parent location for item"));

        var itemType = GetItemBaseType(node);
        var alias = node.GetAlias();

        return CreateItem(alias, parent ?? treeItem, itemType);
    }

    private EntityContainer? TryCreateContainer(string name, ITreeEntity parent)
    {
        var children = entityService.GetChildren(parent.Id, containerType);
        if (children != null && children.Any(x => x.Name.InvariantEquals(name)))
        {
            var item = children.Single(x => x.Name.InvariantEquals(name));
            return FindContainer(item.Key);
        }

        // else create 
        var attempt = CreateContainer(parent.Id, name);
        if (attempt)
            return attempt.Result?.Entity;

        return null;
    }


    #region Getters
    // Getters - get information we already know (either in the object or the XElement)

    protected XElement? GetFolderNode(TObject item)
    {
        if (item.ParentId <= 0) return default;
        // return GetFolderNode(GetContainers(item));

        if (!_folderCache.ContainsKey(item.ParentId))
        {
            var node = GetFolderNode(GetContainers(item));
            if (node is not null) _folderCache[item.ParentId] = node;
        }
        return _folderCache[item.ParentId];
    }

    protected abstract IEnumerable<EntityContainer> GetContainers(TObject item);

    protected XElement? GetFolderNode(IEnumerable<EntityContainer> containers)
    {
        if (containers == null || !containers.Any())
            return default;

        var containerList = containers; // .ToList();

        var folders = containerList.OrderBy(x => x.Level)
            .Select(x => HttpUtility.UrlEncode(x.Name))
            .ToList();

        if (folders.Count != 0)
        {
            var path = string.Join("/", folders);
            return new XElement("Folder", path);
        }

        return default;

    }

    #endregion

    #region Finders

    protected abstract EntityContainer FindContainer(Guid key);
    protected abstract IEnumerable<EntityContainer> FindContainers(string folder, int level);
    protected abstract Attempt<OperationResult<OperationResultType, EntityContainer>> CreateContainer(int parentId, string name);

    protected virtual EntityContainer? FindFolder(Guid key, string path)
    {
        var container = FindContainer(key);
        if (container is not null) return container;

        /// else - we have to parse it like a path ... 
        var bits = path.Split('/');

        var rootFolder = HttpUtility.UrlDecode(bits[0]);

        var root = FindContainers(rootFolder, 1)
            .FirstOrDefault();
        if (root == null)
        {
            var attempt = CreateContainer(-1, rootFolder);
            if (!attempt)
            {
                return null;
            }

            root = attempt.Result?.Entity;
        }

        if (root is not null)
        {
            var current = root;
            for (int i = 1; i < bits.Length; i++)
            {
                var name = HttpUtility.UrlDecode(bits[i]);
                current = TryCreateContainer(name, current);
                if (current is null) break;
            }

            if (current is not null)
            {
                logger.LogDebug("Folder Found {name}", current.Name);
                return current;
            }
        }

        return null;
    }

    #endregion

    #region Container stuff
    protected abstract void SaveContainer(EntityContainer container);
    #endregion


    #region container folder cache 

    /// <summary>
    ///  Container folder cache, makes lookups of items in containers slightly faster.
    /// </summary>
    /// <remarks>
    ///  only used on serialization, allows us to only build the folder path for a set of containers once.
    /// </remarks>
    private Dictionary<int, XElement> _folderCache = [];

    private void ClearFolderCache() 
        => _folderCache = [];

    public void InitializeCache()
    {
        ClearFolderCache();
    }

    public void DisposeCache()
    {
        ClearFolderCache();
    }
    #endregion
}
