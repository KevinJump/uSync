using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.Core;
using uSync.Core.Extensions;
using uSync.Core.Models;

namespace uSync.BackOffice.SyncHandlers;

/// <summary>
///  Base class for any Handlers that manage IEntity type objects
/// </summary>
public abstract class SyncHandlerBase<TObject>
    : SyncHandlerRoot<TObject, IEntity>, ISyncCleanEntryHandler
    where TObject : IEntity
{

    /// <summary>
    /// reference to Umbraco Entity service
    /// </summary>
    protected readonly IEntityService entityService;


    /// <inheritdoc/>
    public SyncHandlerBase(
        ILogger<SyncHandlerBase<TObject>> logger,
        IEntityService entityService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory syncItemFactory)
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
    {
        this.entityService = entityService;
    }

    /// <inheritdoc />
    public override Task<bool> HasChildrenAsync(IEntity item)
        => Task.FromResult(entityService.GetChildren(item.Id).Any());

    /// <summary>
    ///  given a folder we calculate what items we can remove, because they are 
    ///  not in one the the files in the folder.
    /// </summary>
    protected override async Task<IEnumerable<uSyncAction>> CleanFolderAsync(string cleanFile, bool reportOnly, bool flat)
    {
        var folder = Path.GetDirectoryName(cleanFile);
        if (folder is null || syncFileService.DirectoryExists(folder) is false) return [];


        // get the keys for every item in this folder. 

        // this would works on the flat folder structure too, 
        // there we are being super defensive, so if an item
        // is anywhere in the folder it won't get removed
        // even if the folder is wrong
        // be a little slower (not much though)

        // we cache this, (it is cleared on an ImportAll)
        var keys = await GetFolderKeysAsync(folder, flat);
        if (keys.Count > 0)
        {
            // move parent to here, we only need to check it if there are files.
            var parentKey = await GetCleanParentKeyAsync(cleanFile);
            if (parentKey is null) return [];

            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Got parent with {Id} from clean file {file}", parentKey, Path.GetFileName(cleanFile));

            // keys should aways have at least one entry (the key from cleanFile)
            // if it doesn't then something might have gone wrong.
            // because we are being defensive when it comes to deletes, 
            // we only then do deletes when we know we have loaded some keys!
            return await DeleteMissingItemsAsync(parentKey.Value, keys, reportOnly);
        }
        else
        {
            logger.LogWarning("Failed to get the keys for items in the folder, there might be a disk issue {folder}", folder);
            return [];
        }
    }

    private async Task<Guid?> GetCleanParentKeyAsync(string cleanFile)
    {
        // stream the key rather than parsing the whole file, and reuse it for the
        // parent lookup so we don't read the clean file a second time.
        var key = await syncFileService.LoadKeyFromFileAsync(cleanFile);
        if (key == Guid.Empty) return Guid.Empty;
        return (await GetFromServiceAsync(key))?.Key;
    }

    /// <summary>
    ///  Process any cleanup actions that may have been loaded up
    /// </summary>
    public virtual async Task<IEnumerable<uSyncAction>> ProcessCleanActionsAsync(string? folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
    {
        if (folder is null) return [];

        var cleans = actions.Where(x => x.Change == ChangeType.Clean && !string.IsNullOrWhiteSpace(x.FileName)).ToList();
        if (cleans.Count == 0) return [];

        var results = new List<uSyncAction>();

        foreach (var clean in cleans)
        {
            if (!string.IsNullOrWhiteSpace(clean.FileName))
                results.AddRange(await CleanFolderAsync(clean.FileName, false, config.UseFlatStructure));
        }

        return results;
    }

    /// <inheritdoc/>
    protected override async Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(TObject parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
        => await DeleteMissingItemsAsync(parent?.Key ?? Guid.Empty, keysToKeep, reportOnly);

    /// <inheritdoc/>
    protected override async Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(Guid key, IEnumerable<Guid> keysToKeep, bool reportOnly)
    {
        var items = (await GetChildItemsAsync(key)).ToArray();

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("DeleteMissingItems: {parentId} Checking {itemCount} items for {keyCount} keys", key, items.Length, keysToKeep.Count());

        var actions = new List<uSyncAction>();
        foreach (var item in items.Where(x => !keysToKeep.Contains(x.Key)))
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("DeleteMissingItems: Found {item} that is not in file list (Reporting: {reportOnly})", item.Id, reportOnly);

            var name = String.Empty;
            if (item is IEntitySlim slim) name = slim.Name;

            if (string.IsNullOrEmpty(name) || !reportOnly)
            {
                var actualItem = await GetFromServiceAsync(item.Key);
                if (actualItem == null)
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug("Actual Item {id} can't be found", item.Id);

                    continue;
                }

                name = GetItemName(actualItem);

                // actually do the delete if we are really not reporting
                if (!reportOnly)
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug("Deleting item: {id} {name} as part of a 'clean' import", actualItem.Id, name);

                    await DeleteViaServiceAsync(actualItem);
                }
            }

            // for reporting - we use the entity name,
            // this stops an extra lookup - which we may not need later
            actions.Add(
                uSyncActionHelper<TObject>.SetAction(SyncAttempt<TObject>.Succeed(name, ChangeType.Delete), string.Empty, item.Key, this.Alias));
        }

        return actions;
    }

    /// <inheritdoc/>
    protected override async Task<IEnumerable<IEntity>> GetChildItemsAsync(IEntity? parent)
        => await GetChildItemsAsync(parent?.Key ?? Guid.Empty);

    /// <summary>
    ///  Get all child items beneath a given item
    /// </summary>
    virtual protected async Task<IEnumerable<IEntity>> GetChildItemsAsync(Guid key)
    {
        if (this.ItemObjectType == UmbracoObjectTypes.Unknown) return [];
        return await GetChildItemsAsync(key, this.ItemObjectType);
    }

    /// <summary>
    ///  Get all child items beneath a given item
    /// </summary>
    virtual protected async Task<IEnumerable<IEntity>> GetChildItemsAsync(Guid key, UmbracoObjectTypes objectType)
    {
        var cacheKey = $"{GetCacheKeyBase()}_parent_{key}_{objectType}";

        return await runtimeCache.GetCacheItemAsync(cacheKey, async () =>
        {
            return await GetEntityChildrenAsync(key, objectType);
        }, null) ?? [];
    }

    /// <summary>
    ///  Get all child items beneath a given key, for a given object type
    /// </summary>
    private Task<IEnumerable<IEntity>> GetEntityChildrenAsync(Guid key, UmbracoObjectTypes objectType)
    {
        return uSyncTaskHelper.FromResultOf<IEnumerable<IEntity>>(() => {

            // logger.LogDebug("Cache miss [{key}]", cacheKey);
            if (key == Guid.Empty)
            {
                var result = entityService.GetChildren(-1, objectType);
                return result;
            }
            else
            {
                // If you ask for the type then you get more info, and there is extra db calls to 
                // load it, so GetChildren without the object type is quicker. 

                var item = entityService.Get(key);
                if (item is null) return [];
                // but we need to know that we only get our type so we then filter.
                var guidType = ObjectTypes.GetGuid(objectType);
                return entityService.GetChildren(item.Id).Where(x => x.NodeObjectType == guidType);
            }
        });
    }

    /// <summary>
    /// Get all 'folders' beneath a given item (usually these are Container items)
    /// </summary>
    virtual protected async Task<IEnumerable<IEntity>> GetFoldersAsync(Guid key)
    {
        if (this.ItemContainerType == UmbracoObjectTypes.Unknown)
            return await Task.FromResult(Enumerable.Empty<IEntity>());

        return await Task.FromResult<IEnumerable<IEntity>>(entityService.GetChildren(key != Guid.Empty ? key : null, this.ItemContainerType));
    }

    /// <inheritdoc/>
    protected override async Task<IEnumerable<IEntity>> GetFoldersAsync(IEntity? parent)
    {
        if (parent is null) return await GetFoldersAsync(Guid.Empty);
        return await GetFoldersAsync(parent.Key);
    }

    /// <inheritdoc/>
    protected override async Task<TObject?> GetFromServiceAsync(IEntity? entity)
        => entity is null ? default : await GetFromServiceAsync(entity.Key);

    /// <summary>
    ///  for backwards compatibility up the tree.
    /// </summary>
    public async Task<bool> HasChildrenAsync(Guid key)
        => (await GetFoldersAsync(key)).Any() || (await GetChildItemsAsync(key)).Any();

}
