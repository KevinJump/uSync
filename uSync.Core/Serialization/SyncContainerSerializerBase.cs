using System.Collections.Concurrent;
using System.Web;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Extensions;

using uSync.Core.Models;

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

    protected override async Task<SyncAttempt<TObject>> ProcessDeleteAsync(Guid key, string alias, SerializerFlags flags)
    {
        if (flags.HasFlag(SerializerFlags.LastPass))
        {
            logger.LogDebug("Processing deletes as part of the last pass");
            return await base.ProcessDeleteAsync(key, alias, flags);
        }

        logger.LogDebug("Delete not processing as this is not the final pass");
        return SyncAttempt<TObject>.Succeed(alias, ChangeType.Hidden);
    }

    protected override async Task<Attempt<TObject?>> FindOrCreateAsync(XElement node)
    {
        TObject? item = await FindItemAsync(node);
        if (item is not null) return Attempt.Succeed(item);

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
            parent = await FindItemAsync(parentKey, parentNode.Value);
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

                var container = await FindFolderAsync(folderKey, folder.Value);
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
                            await SaveContainerAsync(container);
                        }
                    }
                }
            }
        }

        var itemType = GetItemBaseType(node);
        var alias = node.GetAlias();

        return await CreateItemAsync(alias, parent ?? treeItem, itemType);
    }

    private async Task<EntityContainer?> TryCreateContainerAsync(string name, ITreeEntity parent)
    {
        var children = entityService.GetChildren(parent.Id, containerType);
        if (children != null && children.Any(x => x.Name.InvariantEquals(name)))
        {
            var item = children.Single(x => x.Name.InvariantEquals(name));
            return await FindContainerAsync(item.Key);
        }

        // else create 
        var attempt = await CreateContainerAsync(parent.Key, name);
        if (attempt)
            return attempt.Result;

        return null;
    }


    #region Getters
    // Getters - get information we already know (either in the object or the XElement)

    [Obsolete("Use GetFolderNode will be removed in v16")]
    protected XElement? GetFolderNode(TObject item) 
        => GetFolderNodeAsync(item).Result;

    protected async Task<XElement?> GetFolderNodeAsync(TObject item)
    {
        if (item.ParentId <= 0) return default;
        // return GetFolderNode(GetContainers(item));

        if (_folderCache.TryGetValue(item.ParentId, out var folder) && folder is not null)
            return folder;

        var node = GetFolderNode(await GetContainersAsync(item));
        if (node is not null)
        {
            _folderCache.TryAdd(item.ParentId, node);
            return node;
        }

        return default;
    }

    [Obsolete("Use GetContainers will be removed in v16")]
    protected virtual IEnumerable<EntityContainer> GetContainers(TObject item)
        => GetContainersAsync(item).Result;

    protected abstract Task<IEnumerable<EntityContainer>> GetContainersAsync(TObject item);

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

    protected abstract Task<EntityContainer?> FindContainerAsync(Guid key);

    protected abstract Task<IEnumerable<EntityContainer>> FindContainersAsync(string folder, int level);

    protected abstract Task<Attempt<EntityContainer?, EntityContainerOperationStatus>> CreateContainerAsync(Guid parentKey, string name);

    protected virtual async Task<EntityContainer?> FindFolderAsync(Guid key, string path)
    {
        var container = await FindContainerAsync(key);
        if (container is not null) return container;

        /// else - we have to parse it like a path ... 
        var bits = path.Split('/');

        var rootFolder = HttpUtility.UrlDecode(bits[0]);

        var root = (await FindContainersAsync(rootFolder, 1))
            .FirstOrDefault();
        if (root == null)
        {
            var attempt = await CreateContainerAsync(Guid.Empty, rootFolder);
            if (!attempt)
            {
                return null;
            }

            root = attempt.Result;
        }

        if (root is not null)
        {
            var current = root;
            for (int i = 1; i < bits.Length; i++)
            {
                var name = HttpUtility.UrlDecode(bits[i]);
                current = await TryCreateContainerAsync(name, current);
                if (current is null) break;
            }

            if (current is not null)
            {
                return current;
            }
        }

        return null;
    }

    #endregion

    #region Container stuff
    protected abstract Task SaveContainerAsync(EntityContainer container);

    #endregion


    #region container folder cache 

    /// <summary>
    ///  Container folder cache, makes lookups of items in containers slightly faster.
    /// </summary>
    /// <remarks>
    ///  only used on serialization, allows us to only build the folder path for a set of containers once.
    /// </remarks>
    private ConcurrentDictionary<int, XElement> _folderCache = [];

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


    //[Obsolete("Use FindItemAsync will be removed in v16")]
    //protected virtual EntityContainer? FindFolder(Guid key, string path)
    //    => FindFolderAsync(key, path).Result;
    //[Obsolete("Use FindItemAsync will be removed in v16")]
    //protected abstract EntityContainer? FindContainer(Guid key);
    //[Obsolete("Use FindItemAsync will be removed in v16")]
    //protected abstract IEnumerable<EntityContainer> FindContainers(string folder, int level);
    //[Obsolete("Use FindItemAsync will be removed in v16")]
    //protected abstract Attempt<OperationResult<OperationResultType, EntityContainer>?> CreateContainer(int parentId, string name);
    //[Obsolete("Use SaveItemAsync will be removed in v16")]
    //protected abstract void SaveContainer(EntityContainer container);

}
