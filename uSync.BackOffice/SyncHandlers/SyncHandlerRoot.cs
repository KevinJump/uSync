using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Org.BouncyCastle.Utilities.IO.Pem;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.BackOffice.SyncHandlers;

/// <summary>
/// Root base class for all handlers 
/// </summary>
/// <remarks>
/// If the Handler manages something that Implements IEntity use SyncBaseHandler
/// </remarks>
public abstract class SyncHandlerRoot<TObject, TContainer>
{
    /// <summary>
    /// Reference to the Logger
    /// </summary>
    protected readonly ILogger<SyncHandlerRoot<TObject, TContainer>> logger;

    /// <summary>
    /// Reference to the uSyncFileService
    /// </summary>
    protected readonly ISyncFileService syncFileService;

    /// <summary>
    /// Reference to the Event service used to handle locking
    /// </summary>
    protected readonly ISyncEventService _mutexService;

    /// <summary>
    /// List of dependency checkers for this Handler 
    /// </summary>
    protected readonly IList<ISyncDependencyChecker<TObject>> dependencyCheckers;

    /// <summary>
    /// List of change trackers for this handler 
    /// </summary>
    protected readonly IList<ISyncTracker<TObject>> trackers;

    /// <summary>
    /// The Serializer to use for importing/exporting items
    /// </summary>
    protected ISyncSerializer<TObject> serializer;

    /// <summary>
    /// Runtime cache for caching lookups
    /// </summary>
    protected readonly IAppPolicyCache runtimeCache;

    /// <summary>
    ///  Alias of the handler, used when getting settings from the configuration file
    /// </summary>
    public string Alias { get; private set; }

    /// <summary>
    ///  Name of handler, displayed to user during reports/imports 
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    ///  name of the folder inside the uSync folder where items are stored
    /// </summary>
    public string DefaultFolder { get; private set; }

    /// <summary>
    ///  priority order items are imported in
    /// </summary>
    /// <remarks>
    ///   to import before anything else go below USYNC_RESERVED_LOWER (1000)
    ///   to import after uSync has done all the other things go past USYNC_RESERVED_UPPER (2000)
    /// </remarks>
    public int Priority { get; private set; }

    /// <summary>
    ///  Icon displayed on the screen while the import happens.
    /// </summary>
    public string Icon { get; private set; }

    /// <summary>
    ///  does this handler require two passes at the import (e.g data-types import once, and then again after doc-types)
    /// </summary>
    protected bool IsTwoPass = false;

    /// <summary>
    ///  the object type of the item being processed.
    /// </summary>
    public string ItemType { get; protected set; } = typeof(TObject).Name;

    /// <summary>
    ///  Is the handler enabled 
    /// </summary>
    public bool Enabled { get; set; } = true;


    /// <summary>
    ///  the default configuration for this handler 
    /// </summary>
    public HandlerSettings DefaultConfig { get; set; }

    /// <summary>
    ///  the root folders to use for the handler (based on settings).
    /// </summary>
    protected string[] RootFolders { get; set; }

    /// <summary>
    ///  the UDIEntityType for the handler objects
    /// </summary>
    public string EntityType { get; protected set; }

    /// <summary>
    ///  Name of the type (object)
    /// </summary>
    public string TypeName { get; protected set; }  // we calculate these now based on the entityType ? 

    /// <summary>
    ///  UmbracoObjectType of items handled by this handler
    /// </summary>
    protected UmbracoObjectTypes ItemObjectType { get; set; } = UmbracoObjectTypes.Unknown;

    /// <summary>
    /// UmbracoObjectType of containers managed by this handler
    /// </summary>
    protected UmbracoObjectTypes ItemContainerType = UmbracoObjectTypes.Unknown;

    /// <summary>
    ///  The type of the handler 
    /// </summary>
    protected string handlerType;

    /// <summary>
    ///  SyncItem factory reference
    /// </summary>
    protected readonly ISyncItemFactory itemFactory;

    /// <summary>
    /// Reference to the uSyncConfigService
    /// </summary>
    protected readonly uSyncConfigService uSyncConfig;

    /// <summary>
    /// Umbraco's shortStringHelper
    /// </summary>
    protected readonly IShortStringHelper shortStringHelper;

    /// <summary>
    ///  Constructor, base for all handlers
    /// </summary>
    public SyncHandlerRoot(
            ILogger<SyncHandlerRoot<TObject, TContainer>> logger,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            ISyncFileService syncFileService,
            ISyncEventService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncItemFactory itemFactory)
    {
        this.uSyncConfig = uSyncConfig;

        this.logger = logger;
        this.shortStringHelper = shortStringHelper;
        this.itemFactory = itemFactory;

        var _serializer = this.itemFactory.GetSerializers<TObject>().FirstOrDefault()
            ?? throw new KeyNotFoundException($"No Serializer found for handler {this.Alias}");

        this.serializer = _serializer;
        this.trackers = this.itemFactory.GetTrackers<TObject>().ToList();
        this.dependencyCheckers = this.itemFactory.GetCheckers<TObject>().ToList();


        this.syncFileService = syncFileService;
        this._mutexService = mutexService;

        var currentHandlerType = GetType();
        var meta = currentHandlerType.GetCustomAttribute<SyncHandlerAttribute>(false) ??
            throw new InvalidOperationException($"The Handler {handlerType} requires a {typeof(SyncHandlerAttribute)}");

        handlerType = currentHandlerType.ToString();
        Name = meta.Name;
        Alias = meta.Alias;
        DefaultFolder = meta.Folder;
        Priority = meta.Priority;
        IsTwoPass = meta.IsTwoPass;
        Icon = string.IsNullOrWhiteSpace(meta.Icon) ? "icon-umb-content" : meta.Icon;
        EntityType = meta.EntityType;

        TypeName = serializer.ItemType;

        this.ItemObjectType = uSyncObjectType.ToUmbracoObjectType(EntityType);
        this.ItemContainerType = uSyncObjectType.ToContainerUmbracoObjectType(EntityType);

        this.DefaultConfig = GetDefaultConfig();
        RootFolders = uSyncConfig.GetFolders();

        if (uSyncConfig.Settings.CacheFolderKeys)
        {
            this.runtimeCache = appCaches.RuntimeCache;
        }
        else
        {
            logger.LogInformation("No caching of handler key lookups (CacheFolderKeys = false)");
            this.runtimeCache = NoAppCache.Instance;
        }
    }

    private HandlerSettings GetDefaultConfig()
    {
        var defaultSet = uSyncConfig.GetDefaultSetSettings();
        var config = defaultSet.GetHandlerSettings(this.Alias);

        if (defaultSet.DisabledHandlers.InvariantContains(this.Alias))
            config.Enabled = false;

        return config;
    }

    #region Importing 

    /// <summary>
    ///  import everything from a collection of folders, using the supplied config.
    /// </summary>
    /// <remarks>
    ///  allows us to 'merge' a collection of folders down and perform an import against them (without first having to actually merge the folders on disk)
    /// </remarks>
   
    public async Task<IEnumerable<uSyncAction>> ImportAllAsync(string[] folders, HandlerSettings config, uSyncImportOptions options)
    {
        var cacheKey = PrepCaches();
        runtimeCache.ClearByKey(cacheKey);

        options.Callbacks?.Update?.Invoke("Calculating import order", 1, 9);

        var items = await GetMergedItemsAsync(folders);

        options.Callbacks?.Update?.Invoke($"Processing {items.Count} items", 2, 9);

        // create the update list with items.count space. this is the max size we need this list. 
        List<uSyncAction> actions = new(items.Count);
        List<ImportedItem<TObject>> updates = new(items.Count);
        List<string> cleanMarkers = [];

        int count = 0;
        int total = items.Count;

        foreach (var item in items)
        {
            count++;

            options.Callbacks?.Update?.Invoke($"Importing {Path.GetFileNameWithoutExtension(item.Path)}", count, total);

            var result = await ImportElementAsync(item.Node, item.Path, config, options);
            foreach (var attempt in result)
            {
                if (attempt.Success)
                {
                    if (attempt.Change == ChangeType.Clean)
                    {
                        cleanMarkers.Add(item.Path);
                    }
                    else if (attempt.Item is not null && attempt.Item is TObject update)
                    {
                        updates.Add(new ImportedItem<TObject>(item.Node, update));
                    }
                }

                if (attempt.Change != ChangeType.Clean)
                    actions.Add(attempt);
            }
        }

        // clean up memory we didn't use in the update list. 
        updates.TrimExcess();

        // bulk save?
        if (updates.Count > 0)
        {
            if (options.Flags.HasFlag(SerializerFlags.DoNotSave))
            {
                await serializer.SaveAsync(updates.Select(x => x.Item));
            }

            await PerformSecondPassImportsAsync(updates, actions, config, options.Callbacks?.Update);
        }

        if (actions.All(x => x.Success) && cleanMarkers.Count > 0)
        {
            await PerformImportCleanAsync(cleanMarkers, actions, config, options.Callbacks?.Update);
        }

        CleanCaches(cacheKey);
        options.Callbacks?.Update?.Invoke("Done", 3, 3);

        logger.LogDebug("ImportAll: {count} items imported", actions.Count);

        return actions;
    }

    /// <summary>
    ///  get all items for the report/import process.
    /// </summary>
    /// <param name="folders"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<OrderedNodeInfo>> FetchAllNodesAsync(string[] folders)
        => await GetMergedItemsAsync(folders);

    /// <summary>
    ///  method to get the merged folders, handlers that care about orders should override this. 
    /// </summary>
    protected virtual async Task<IReadOnlyList<OrderedNodeInfo>> GetMergedItemsAsync(string[] folders)
    {
        var baseTracker = trackers.FirstOrDefault() as ISyncTrackerBase;
        return (await syncFileService.MergeFoldersAsync(folders, uSyncConfig.Settings.DefaultExtension, baseTracker))
            .ToArray();
    }

    /// <summary>
    ///  given a file path, will give you the merged values across all folders. 
    /// </summary>
    protected virtual async Task<XElement?> GetMergedNodeAsync(string filePath)
    {
        var allFiles = uSyncConfig.GetFolders()
            .Select(x => syncFileService.GetAbsPath($"{x}/{this.DefaultFolder}/{filePath}"))
            .ToArray();

        var baseTracker = trackers.FirstOrDefault() as ISyncTrackerBase;
        return await syncFileService.MergeFilesAsync(allFiles, baseTracker);
    }


    private async Task PerformImportCleanAsync(List<string> cleanMarkers, List<uSyncAction> actions, HandlerSettings config, SyncUpdateCallback? callback)
    {
        foreach (var item in cleanMarkers.Select((filePath, Index) => new { filePath, Index }))
        {
            var folderName = Path.GetFileName(item.filePath);
            callback?.Invoke($"Cleaning {folderName}", item.Index, cleanMarkers.Count);

            var cleanActions = await CleanFolderAsync(item.filePath, false, config.UseFlatStructure);
            if (cleanActions.Any())
            {
                actions.AddRange(cleanActions);
            }
            else
            {
                // nothing to delete, we report this as a no change 
                actions.Add(uSyncAction.SetAction(
                    success: true,
                    name: $"Folder {Path.GetFileName(item.filePath)}",
                    change: ChangeType.NoChange,
                    filename: syncFileService.GetSiteRelativePath(item.filePath)));
            }
        }
        // remove the actual cleans (they will have been replaced by the deletes
        actions.RemoveAll(x => x.Change == ChangeType.Clean);
    }

    /// <summary>
    ///  Import a single item, from the .config file supplied
    /// </summary>
    [Obsolete("Use ImportElementAsync will be removed in v16")]
    public virtual IEnumerable<uSyncAction> Import(string filePath, HandlerSettings config, SerializerFlags flags)
        => ImportAsync(filePath, config, new uSyncImportOptions { Flags = flags }).Result;

    public virtual async Task<IEnumerable<uSyncAction>> ImportAsync(string filePath, HandlerSettings config, uSyncImportOptions options)
    {
        try
        {
            syncFileService.EnsureFileExists(filePath);
            var node = await syncFileService.LoadXElementAsync(filePath);
            return await ImportElementAsync(node, filePath, config, options);
        }
        catch (FileNotFoundException notFoundException)
        {
            return uSyncAction.Fail(Path.GetFileName(filePath), this.handlerType, this.ItemType, ChangeType.Fail, $"File not found {notFoundException.Message}", notFoundException)
                .AsEnumerableOfOne();
        }
        catch (Exception ex)
        {
            logger.LogWarning("[{alias}] Import Failed : {exception}", this.Alias, ex.ToString());
            return uSyncAction.Fail(Path.GetFileName(filePath), this.handlerType, this.ItemType, ChangeType.Fail, $"Import Fail: {ex.Message}", new Exception(ex.Message, ex))
                .AsEnumerableOfOne();
        }
    }

    /// <summary>
    ///  Import a single item from a usync XML file
    /// </summary>
    virtual public async Task<IEnumerable<uSyncAction>> ImportAsync(string file, HandlerSettings config, bool force)
    {
        var flags = SerializerFlags.OnePass;
        if (force) flags |= SerializerFlags.Force;

        var options = new uSyncImportOptions
        {
            Flags = flags
        };

        if (file.InvariantStartsWith($"{uSyncConstants.MergedFolderName}/"))
        {
            var node = await GetMergedNodeAsync(file.Substring(uSyncConstants.MergedFolderName.Length + 1));
            if (node is not null)
                return await ImportElementAsync(node, file, config, options);
            else
                throw new Exception("Unable to merge files from root folder");
        }

        return await ImportAsync(file, config, options);
    }

    /// <summary>
    /// Import a node, with settings and options 
    /// </summary>
    /// <remarks>
    ///  All Imports lead here
    /// </remarks>
    virtual public async Task<IEnumerable<uSyncAction>> ImportElementAsync(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options)
    {
        if (!await ShouldImportAsync(node, settings))
        {
            return uSyncAction.SetAction(true, node.GetAlias(), message: "Change blocked (based on configuration)")
                .AsEnumerableOfOne();
        }

        if (await _mutexService.FireItemStartingEventAsync(new uSyncImportingItemNotification(node, (ISyncHandler)this)))
        {
            // blocked
            return uSyncActionHelper<TObject>
                .ReportAction(ChangeType.NoChange, node.GetAlias(), node.GetPath(), GetNameFromFileOrNode(filename, node), node.GetKey(), this.Alias, "Change stopped by delegate event")
                .AsEnumerableOfOne();
        }

        try
        {
            // merge the options from the handler and any import options into our serializer options.
            var serializerOptions = new SyncSerializerOptions(options.Flags, settings.Settings, options.UserId);
            serializerOptions.MergeSettings(options.Settings);

            // get the item.
            var attempt = await DeserializeItemAsync(node, serializerOptions);
            var action = uSyncActionHelper<TObject>.SetAction(attempt, GetNameFromFileOrNode(filename, node), node.GetKey(), this.Alias, IsTwoPass);

            // add item if we have it.
            if (attempt.Item != null) action.Item = attempt.Item;

            // add details if we have them
            if (attempt.Details != null && attempt.Details.Any()) action.Details = attempt.Details;

            // this might not be the place to do this because, two pass items are imported at another point too.
            await _mutexService.FireItemCompletedEventAsync(new uSyncImportedItemNotification(node, attempt.Change));

            return action.AsEnumerableOfOne();
        }
        catch (Exception ex)
        {
            logger.LogWarning("[{alias}] ImportElement Failed : {exception}", this.Alias, ex.ToString());
            return uSyncAction.Fail(Path.GetFileName(filename), this.Alias, this.ItemType, ChangeType.Fail,
                $"{this.Alias} Import Fail: {ex.Message}", new Exception(ex.Message))
                .AsEnumerableOfOne();
        }

    }


    /// <summary>
    ///  Works through a list of items that have been processed and performs the second import pass on them.
    /// </summary>
    private async Task PerformSecondPassImportsAsync(List<ImportedItem<TObject>> importedItems, List<uSyncAction> actions, HandlerSettings config, SyncUpdateCallback? callback = null)
    {
        foreach (var item in importedItems.Select((update, Index) => new { update, Index }))
        {
            var itemKey = item.update.Node.GetKey();

            callback?.Invoke($"Second Pass {item.update.Node.GetKey()}", item.Index, importedItems.Count);
            var attempt = await ImportSecondPassAsync(item.update.Node, item.update.Item, config, callback);
            if (attempt.Success)
            {
                // if the second attempt has a message on it, add it to the first attempt.
                if (!string.IsNullOrWhiteSpace(attempt.Message) || attempt.Details?.Any() == true)
                {
                    // uSyncAction action = actions.FirstOrDefault(x => $"{x.key}_{x.HandlerAlias}" == $"{itemKey}_{this.Alias}", new uSyncAction { key = Guid.Empty });
                    if (actions.TryFindAction(itemKey, this.Alias, out var action))
                    {
                        if (action.Key != Guid.Empty)
                        {
                            actions.Remove(action);
                            action.Message += attempt.Message ?? "";

                            if (attempt.Details?.Any() == true)
                            {
                                var details = action.Details?.ToList() ?? [];
                                details.AddRange(attempt.Details);
                                action.Details = details;
                            }
                            actions.Add(action);
                        }
                    }
                }
                if (attempt.Change > ChangeType.NoChange && !attempt.Saved && attempt.Item != null)
                {
                    await serializer.SaveAsync(attempt.Item.AsEnumerableOfOne());
                }
            }
            else
            {
                if (actions.TryFindAction(itemKey, this.Alias, out var action))
                {
                    actions.Remove(action);
                    action.Success = attempt.Success;
                    action.Message = $"Second Pass Fail: {attempt.Message}";
                    action.Exception = attempt.Exception;
                    actions.Add(action);
                }
            }
        }
    }

    /// <summary>
    /// Perform a second pass import on an item
    /// </summary>
    virtual public async Task<IEnumerable<uSyncAction>> ImportSecondPassAsync(uSyncAction action, HandlerSettings settings, uSyncImportOptions options)
    {
        if (!IsTwoPass) return [];

        try
        {
            var fileName = action.FileName;

            if (fileName is null || syncFileService.FileExists(fileName) is false) return [];

            var node = await syncFileService.LoadXElementAsync(fileName);
            var item = await GetFromServiceAsync(node.GetKey());
            if (item is null) return [];

            // merge the options from the handler and any import options into our serializer options.
            var serializerOptions = new SyncSerializerOptions(options?.Flags ?? SerializerFlags.None, settings.Settings, options?.UserId ?? -1);
            serializerOptions.MergeSettings(options?.Settings);

            // do the second pass on this item
            var result = await DeserializeItemSecondPassAsync(item, node, serializerOptions);

            return uSyncActionHelper<TObject>.SetAction(result, syncFileService.GetSiteRelativePath(fileName), node.GetKey(), this.Alias).AsEnumerableOfOne();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Second Import Failed");
            return uSyncAction.Fail(action.Name, this.handlerType, action.ItemType, ChangeType.ImportFail, "Second import failed", ex).AsEnumerableOfOne();
        }
    }



    /// <summary>
    ///  Perform a 'second pass' import on a single item.
    /// </summary>
    virtual public async Task<SyncAttempt<TObject>> ImportSecondPassAsync(XElement node, TObject item, HandlerSettings config, SyncUpdateCallback? callback)
    {
        if (IsTwoPass is false)
            return SyncAttempt<TObject>.Succeed(GetItemAlias(item), ChangeType.NoChange);

        try
        {
            return await DeserializeItemSecondPassAsync(item, node, new SyncSerializerOptions(SerializerFlags.None, config.Settings));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Second Import Failed");
            return SyncAttempt<TObject>.Fail(GetItemAlias(item), item, ChangeType.Fail, ex.Message, ex);
        }
    }

    /// <summary>
    ///  given a folder we calculate what items we can remove, because they are 
    ///  not in one the files in the folder.
    /// </summary>
    [Obsolete("use CleanFolderAsync will be removed in v16")]
    protected virtual IEnumerable<uSyncAction> CleanFolder(string cleanFile, bool reportOnly, bool flat)
        => CleanFolderAsync(cleanFile, reportOnly, flat).Result;

    protected virtual async Task<IEnumerable<uSyncAction>> CleanFolderAsync(string cleanFile, bool reportOnly, bool flat)
    {
        var folder = Path.GetDirectoryName(cleanFile);
        if (string.IsNullOrWhiteSpace(folder) is true || syncFileService.DirectoryExists(folder) is false)
            return [];

        // get the keys for every item in this folder. 

        // this would works on the flat folder structure too, 
        // there we are being super defensive, so if an item
        // is anywhere in the folder it won't get removed
        // even if the folder is wrong
        // be a little slower (not much though)

        // we cache this, (it is cleared on an ImportAll)
        var keys = GetFolderKeys(folder, flat);
        if (keys.Count > 0)
        {
            // move parent to here, we only need to check it if there are files.
            var parent = await GetCleanParentAsync(cleanFile);
            if (parent == null) return [];
            logger.LogDebug("Got parent with {alias} from clean file {file}", GetItemAlias(parent), Path.GetFileName(cleanFile));

            // keys should aways have at least one entry (the key from cleanFile)
            // if it doesn't then something might have gone wrong.
            // because we are being defensive when it comes to deletes, 
            // we only then do deletes when we know we have loaded some keys!
            return await DeleteMissingItemsAsync(parent, keys, reportOnly);
        }
        else
        {
            logger.LogWarning("Failed to get the keys for items in the folder, there might be a disk issue {folder}", folder);
            return [];
        }
    }

    /// <summary>
    ///  pre-populates the cache folder key list. 
    /// </summary>
    /// <remarks>
    ///  this means if we are calling the process multiple times, 
    ///  we can optimize the key code and only load it once. 
    /// </remarks>
    public Task PreCacheFolderKeysAsync(string folder, IList<Guid> folderKeys)
    {
        var cacheKey = $"{GetCacheKeyBase()}_{folder.GetHashCode()}";
        runtimeCache.ClearByKey(cacheKey);
        runtimeCache.GetCacheItem(cacheKey, () => folderKeys);

        return Task.CompletedTask;
    }

    /// <summary>
    ///  Get the GUIDs for all items in a folder
    /// </summary>
    /// <remarks>
    ///  This is disk intensive, (checking the .config files all the time)
    ///  so we cache it, and if we are using the flat folder structure, then
    ///  we only do it once, so its quicker. 
    /// </remarks>
    protected IList<Guid> GetFolderKeys(string folder, bool flat)
    {
        // We only need to load all the keys once per handler (if all items are in a folder that key will be used).
        var folderKey = folder.GetHashCode();

        var cacheKey = $"{GetCacheKeyBase()}_{folderKey}";


        return runtimeCache.GetCacheItem(cacheKey, () =>
        {
            logger.LogDebug("Getting Folder Keys : {cacheKey}", cacheKey);

            // when it's not flat structure we also get the sub folders. (extra defensive get them all)
            var keys = new List<Guid>();
            var files = syncFileService.GetFiles(folder, $"*.{this.uSyncConfig.Settings.DefaultExtension}", !flat).ToList();

            foreach (var file in files)
            {
                var node = XElement.Load(file);
                var key = node.GetKey();
                if (key != Guid.Empty && !keys.Contains(key))
                {
                    keys.Add(key);
                }
            }

            logger.LogDebug("Loaded {count} keys from {folder} [{cacheKey}]", keys.Count, folder, cacheKey);

            return keys;

        }, null) ?? [];
    }

    /// <summary>
    ///  Get the parent item of the clean file (so we can check if the folder has any versions of this item in it)
    /// </summary>
    [Obsolete("Use GetCleanParentAsync will be removed in v16")]
    protected TObject? GetCleanParent(string file)
        => GetCleanParentAsync(file).Result;

    protected async Task<TObject?> GetCleanParentAsync(string file)
    {
        var node = XElement.Load(file);
        var key = node.GetKey();
        if (key == Guid.Empty) return default;
        return await GetFromServiceAsync(key);
    }

    /// <summary>
    ///  remove an items that are not listed in the GUIDs to keep
    /// </summary>
    /// <param name="parent">parent item that all keys will be under</param>
    /// <param name="keysToKeep">list of GUIDs of items we don't want to delete</param>
    /// <param name="reportOnly">will just report what would happen (doesn't do the delete)</param>
    /// <returns>list of delete actions</returns>
    [Obsolete("Use DeleteMissingItemsAsync will be removed in v16")]
    protected virtual IEnumerable<uSyncAction> DeleteMissingItems(TObject parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
        => DeleteMissingItemsAsync(parent, keysToKeep, reportOnly).Result;

    protected abstract Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(TObject parent, IEnumerable<Guid> keysToKeep, bool reportOnly);

    /// <summary>
    /// Remove an items that are not listed in the GUIDs to keep.
    /// </summary>
    /// <param name="parentId">parent item that all keys will be under</param>
    /// <param name="keysToKeep">list of GUIDs of items we don't want to delete</param>
    /// <param name="reportOnly">will just report what would happen (doesn't do the delete)</param>
    /// <returns>list of delete actions</returns>
    [Obsolete("use DeleteMissingItemsAsync will be removed in v16")]
    protected virtual IEnumerable<uSyncAction> DeleteMissingItems(int parentId, IEnumerable<Guid> keysToKeep, bool reportOnly)
        => [];

    protected virtual Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(Guid key, IEnumerable<Guid> keysToKeep, bool reportOnly)
        => Task.FromResult(Enumerable.Empty<uSyncAction>());

    /// <summary>
    ///  Get the files we are going to import from a folder. 
    /// </summary>
    protected virtual IEnumerable<string> GetImportFiles(string folder)
        => syncFileService.GetFiles(folder, $"*.{this.uSyncConfig.Settings.DefaultExtension}").OrderBy(x => x);

    /// <summary>
    ///  check to see if this element should be imported as part of the process.
    /// </summary>
    [Obsolete("Use ShouldImportAsync will be removed in v16")]
    virtual protected bool ShouldImport(XElement node, HandlerSettings config)
        => ShouldImportAsync(node, config).Result;

    virtual protected async Task<bool> ShouldImportAsync(XElement node, HandlerSettings config)
    {
        // if createOnly is on, then we only create things that are not already there. 
        // this lookup is slow (relatively) so we only do it if we have to.
        if (config.GetSetting(Core.uSyncConstants.DefaultSettings.CreateOnly, Core.uSyncConstants.DefaultSettings.CreateOnly_Default)
            || config.GetSetting(Core.uSyncConstants.DefaultSettings.OneWay, Core.uSyncConstants.DefaultSettings.CreateOnly_Default))
        {
            var item = await serializer.FindItemAsync(node);
            if (item != null)
            {
                logger.LogDebug("CreateOnly: Item {alias} already exist not importing it.", node.GetAlias());
                return false;
            }
        }


        // Ignore alias setting. 
        // if its set the thing with this alias is ignored.
        var ignore = config.GetSetting<string>("IgnoreAliases", string.Empty);
        if (!string.IsNullOrWhiteSpace(ignore))
        {
            var ignoreList = ignore.ToDelimitedList();
            if (ignoreList.InvariantContains(node.GetAlias()))
            {
                logger.LogDebug("Ignore: Item {alias} is in the ignore list", node.GetAlias());
                return false;
            }
        }


        return true;
    }


    /// <summary>
    ///  Check to see if this element should be exported. 
    /// </summary>
    [Obsolete("Use ShouldExportAsync will be removed in v16")]
    virtual protected bool ShouldExport(XElement node, HandlerSettings config) => true;
    virtual protected Task<bool> ShouldExportAsync(XElement node, HandlerSettings config) => Task.FromResult(true);

    #endregion

    #region Exporting

    /// <summary>
    /// Export all items to a give folder on the disk
    /// </summary>
    [Obsolete("Use ExportAllAsync will be removed in v16")]
    virtual public IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings config, SyncUpdateCallback? callback)
        => ExportAll([folder], config, callback);

    /// <summary>
    /// Export all items to a give folder on the disk
    /// </summary>
    [Obsolete("Use ExportAllAsync will be removed in v16")]
    virtual public IEnumerable<uSyncAction> ExportAll(string[] folders, HandlerSettings config, SyncUpdateCallback? callback)
    {
        // we don't clean the folder out on an export all. 
        // because the actions (renames/deletes) live in the folder
        //
        // there will have to be a different clean option
        // syncFileService.CleanFolder(folder);

        return ExportAll(default, folders, config, callback);
    }

    /// <summary>
    ///  export all items underneath a given container 
    /// </summary>
    [Obsolete("use ExportAllAsync will be removed in v16")]
    virtual public IEnumerable<uSyncAction> ExportAll(TContainer? parent, string folder, HandlerSettings config, SyncUpdateCallback? callback)
        => ExportAll(parent, [folder], config, callback);

    /// <summary>
    /// Export all items to a give folder on the disk
    /// </summary>
    [Obsolete("Use ExportAllAsync will be removed in v16")]
    virtual public IEnumerable<uSyncAction> ExportAll(TContainer? parent, string[] folders, HandlerSettings config, SyncUpdateCallback? callback)
        => ExportAllAsync(parent, folders, config, callback).Result;

    virtual public async Task<IEnumerable<uSyncAction>> ExportAllAsync(string[] folders, HandlerSettings settings, SyncUpdateCallback? callback)
        => await ExportAllAsync(default, folders, settings, callback);


    virtual public async Task<IEnumerable<uSyncAction>> ExportAllAsync(TContainer? parent, string[] folders, HandlerSettings config, SyncUpdateCallback? callback)
    {
        var actions = new List<uSyncAction>();

        if (ItemContainerType != UmbracoObjectTypes.Unknown)
        {
            var containers = await GetFoldersAsync(parent);
            foreach (var container in containers)
            {
                actions.AddRange(await ExportAllAsync(container, folders, config, callback));
            }
        }

        var items = (await GetChildItemsAsync(parent)).ToArray();
        foreach (var item in items.Select((Value, Index) => new { Value, Index }))
        {
            TObject? concreteType;
            if (item.Value is TObject t)
            {
                concreteType = t;
            }
            else
            {
                concreteType = await GetFromServiceAsync(item.Value);
            }
            if (concreteType is not null)
            {  // only export the items (not the containers).
                callback?.Invoke(GetItemName(concreteType), item.Index, items.Length);
                actions.AddRange(await ExportAsync(concreteType, folders, config));
            }
            actions.AddRange(await ExportAllAsync(item.Value, folders, config, callback));
        }

        return actions;
    }

    /// <summary>
    /// Fetch all child items beneath a given container 
    /// </summary>
    [Obsolete("use GetFoldersAsync will be removed in v16")]
    virtual protected IEnumerable<TContainer> GetChildItems(TContainer? parent) => [];

    abstract protected Task<IEnumerable<TContainer>> GetChildItemsAsync(TContainer? parent);

    /// <summary>
    /// Fetch all child items beneath a given folder
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    [Obsolete("use GetFoldersAsync will be removed in v16")]
    virtual protected IEnumerable<TContainer> GetFolders(TContainer? parent)
        => GetFoldersAsync(parent).Result;

    abstract protected Task<IEnumerable<TContainer>> GetFoldersAsync(TContainer? parent);

    /// <summary>
    /// Does this container have any children 
    /// </summary>
    [Obsolete("use GetFoldersAsync will be removed in v16")]
    public bool HasChildren(TContainer item)
        => GetFolders(item).Any() || GetChildItems(item).Any();

    public virtual async Task<bool> HasChildrenAsync(TContainer item)
        => (await GetFoldersAsync(item)).Any() || (await GetChildItemsAsync(item)).Any();

    /// <summary>
    /// Export a single item based on it's ID
    /// </summary>
    [Obsolete("We don't export by id anymore, will be removed in v16")]
    public IEnumerable<uSyncAction> Export(int id, string folder, HandlerSettings settings)
        => Export(id, [folder], settings);

    /// <summary>
    ///  Export an item based on its id, observing root behavior. 
    /// </summary>
    [Obsolete("We don't export by id anymore, will be removed in v16")]
    public IEnumerable<uSyncAction> Export(int id, string[] folders, HandlerSettings settings)
    {
        //var item = this.GetFromService(id);
        //if (item is null)
        //{
        return uSyncAction.Fail(
            id.ToString(), this.handlerType, this.ItemType,
            ChangeType.Export, "Unable to find item",
            new KeyNotFoundException($"Item of {id} cannot be found"))
        .AsEnumerableOfOne();
        //}
        //return this.Export(item, folders, settings);
    }

    /// <summary>
    /// Export an single item from a given UDI value
    /// </summary>
    public async Task<IEnumerable<uSyncAction>> ExportAsync(Udi udi, string[] folders, HandlerSettings settings)
    {
        var item = await FindByUdiAsync(udi);
        if (item != null)
            return await ExportAsync(item, folders, settings);

        if (udi.IsRoot && settings.CreateClean)
        {
            // for roots we still can create a clean
            var targetFolder = folders.Last();
            var filename = Path.Combine(targetFolder, $"{Guid.Empty}.{this.uSyncConfig.Settings.DefaultExtension}");
            await CreateCleanFileAsync(Guid.Empty, filename);
        }


        return uSyncAction.Fail(nameof(udi), this.handlerType, this.ItemType, ChangeType.Fail, $"Item not found {udi}",
             new KeyNotFoundException(nameof(udi)))
            .AsEnumerableOfOne();
    }

    /// <summary>
    /// Export a given item to disk
    /// </summary>
    [Obsolete("export wil folders not a single folder - will be removed v16?")]
    virtual public IEnumerable<uSyncAction> Export(TObject item, string folder, HandlerSettings config)
        => Export(item, [folder], config);

    /// <summary>
    /// Export a given item to disk
    /// </summary>
    [Obsolete("use ExportAsync will be removed in v16")]
    virtual public IEnumerable<uSyncAction> Export(TObject item, string[] folders, HandlerSettings config)
        => ExportAsync(item, folders, config).Result;

    virtual public async Task<IEnumerable<uSyncAction>> ExportAsync(TObject item, string[] folders, HandlerSettings config)
    {
        if (item == null)
            return uSyncAction.Fail(nameof(item), this.handlerType, this.ItemType, ChangeType.Fail, "Item not set",
                new ArgumentNullException(nameof(item))).AsEnumerableOfOne();

        if ( await _mutexService.FireItemStartingEventAsync(new uSyncExportingItemNotification<TObject>(item, (ISyncHandler)this)))
        { 
            return uSyncActionHelper<TObject>
                .ReportAction(ChangeType.NoChange, GetItemName(item), string.Empty, string.Empty, GetItemKey(item), this.Alias,
                                "Change stopped by delegate event")
                .AsEnumerableOfOne();
        }

        var targetFolder = folders.Last();

        var filename = (await GetPathAsync(targetFolder, item, config.GuidNames, config.UseFlatStructure))
            .ToAppSafeFileName();

        // 
        if (IsLockedAtRoot(folders, filename.Substring(targetFolder.Length + 1)))
        {
            // if we have lock roots on, then this item will not export 
            // because exporting would mean the root was no longer used.
            return uSyncAction.SetAction(true, syncFileService.GetSiteRelativePath(filename),
                type: typeof(TObject).ToString(),
                change: ChangeType.NoChange,
                message: "Not exported (would overwrite root value)",
                filename: filename).AsEnumerableOfOne();
        }


        var attempt = await Export_DoExportAsync(item, filename, folders, config);

        if (attempt.Change > ChangeType.NoChange)
            await _mutexService.FireItemCompletedEventAsync(new uSyncExportedItemNotification(attempt.Item, ChangeType.Export));

        return uSyncActionHelper<XElement>.SetAction(attempt, syncFileService.GetSiteRelativePath(filename), GetItemKey(item), this.Alias).AsEnumerableOfOne();
    }

    /// <summary>
    ///  Do the meat of the export 
    /// </summary>
    /// <remarks>
    ///  inheriting this method, means you don't have to repeat all the checks in child handlers. 
    /// </remarks>
    [Obsolete("use DoExportAsync will be removed in v16")]
    protected virtual SyncAttempt<XElement> Export_DoExport(TObject item, string filename, string[] folders, HandlerSettings config)
        => Export_DoExportAsync(item, filename, folders, config).Result;

    protected virtual async Task<SyncAttempt<XElement>> Export_DoExportAsync(TObject item, string filename, string[] folders, HandlerSettings config)
    {
        var attempt = await SerializeItemAsync(item, new SyncSerializerOptions(config.Settings));
        if (attempt.Success && attempt.Item is not null)
        {
            if (await ShouldExportAsync(attempt.Item, config))
            {
                
                var files = await Task.WhenAll(folders
                    .Select(async x =>  await GetPathAsync(x, item, config.GuidNames, config.UseFlatStructure))
                    .ToArray());

                var nodes = await syncFileService.GetAllNodesAsync(files[..^1]);
                if (nodes.Count > 0)
                {
                    nodes.Add(attempt.Item);
                    var differences = syncFileService.GetDifferences(nodes, trackers.FirstOrDefault());
                    if (differences is not null && differences.HasElements)
                    {
                       await syncFileService.SaveXElementAsync(attempt.Item, filename);
                    }
                    else
                    {

                        if (syncFileService.FileExists(filename))
                        {
                            // we don't delete them - because in deployments they might then hang around
                            // we mark them as reverted and then they don't get processed.
                            var emptyNode = XElementExtensions.MakeEmpty(attempt.Item.GetKey(), SyncActionType.None, "Reverted to root");
                            await syncFileService.SaveXElementAsync(emptyNode, filename);
                        }
                    }
                }
                else
                {
                    await syncFileService.SaveXElementAsync(attempt.Item, filename);
                }

                if (config.CreateClean && await HasChildrenAsync(item))
                {
                    await CreateCleanFileAsync(GetItemKey(item), filename);
                }
            }
            else
            {
                return SyncAttempt<XElement>.Succeed(Path.GetFileName(filename), ChangeType.NoChange, "Not Exported (Based on configuration)");
            }
        }

        return attempt;
    }

    /// <summary>
    ///  Checks to see if this item is locked at the root level (meaning we shouldn't save it)
    /// </summary>
    protected bool IsLockedAtRoot(string[] folders, string path)
    {
        if (folders.Length <= 1) return false;

        if (ExistsInFolders(folders[..^1], path.TrimStart(['\\', '/'])))
        {
            return uSyncConfig.Settings.LockRoot || uSyncConfig.Settings.LockRootTypes.InvariantContains(EntityType);
        }

        return false;

        bool ExistsInFolders(string[] folders, string path)
        {
            foreach (var folder in folders)
            {
                if (syncFileService.FileExists(Path.Combine(folder, path.TrimStart(['\\', '/']))))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    ///  does this item have any children ? 
    /// </summary>
    /// <remarks>
    ///  on items where we can check this (quickly) we can reduce the number of checks we might 
    ///  make on child items or cleaning up where we don't need to. 
    /// </remarks>
    [Obsolete("use HasChildrenAsync will be removed in v16")]
    protected virtual bool HasChildren(TObject item)
        => HasChildrenAsync(item).Result;

    protected virtual Task<bool> HasChildrenAsync(TObject item)
        => Task.FromResult(true);

    /// <summary>
    ///  create a clean file, which is used as a marker, when performing remote deletes.
    /// </summary>
    protected async Task CreateCleanFileAsync(Guid key, string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return;

        var folder = Path.GetDirectoryName(filename);
        var name = Path.GetFileNameWithoutExtension(filename);

        if (string.IsNullOrEmpty(folder)) return;

        var cleanPath = Path.Combine(folder, $"{name}_clean.config");

        var node = XElementExtensions.MakeEmpty(key, SyncActionType.Clean, $"clean {name} children");
        node.Add(new XAttribute("itemType", serializer.ItemType));
        await syncFileService.SaveXElementAsync(node, cleanPath);
    }

    #endregion

    #region Reporting 

    public async Task<IEnumerable<uSyncAction>> ReportAsync(string[] folders, HandlerSettings config, SyncUpdateCallback? callback)
    {
        List<uSyncAction> actions = [];

        var cacheKey = PrepCaches();

        callback?.Invoke("Organizing import structure", 1, 3);

        var items = await GetMergedItemsAsync(folders);
        var options = new uSyncImportOptions();

        int count = 0;

        foreach (var item in items)
        {
            count++;
            callback?.Invoke(Path.GetFileNameWithoutExtension(item.Path), count, items.Count);
            actions.AddRange(await ReportElementAsync(item.Node, item.FileName, config, options));
        }

        callback?.Invoke("Validating Report", 2, 3);
        var validationActions = ReportMissingParents([.. actions]);
        actions.AddRange(ReportDeleteCheck(uSyncConfig.GetWorkingFolder(), validationActions));

        CleanCaches(cacheKey);
        callback?.Invoke($"Done ({this.ItemType})", 3, 3);
        return actions;
    }

    /// <summary>
    ///  Check to returned report to see if there is a delete and an update for the same item
    ///  because if there is then we have issues.
    /// </summary>
    protected virtual IEnumerable<uSyncAction> ReportDeleteCheck(string folder, IEnumerable<uSyncAction> actions)
    {
        var duplicates = new List<uSyncAction>();

        // delete checks. 
        foreach (var deleteAction in actions.Where(x => x.Change != ChangeType.NoChange && x.Change == ChangeType.Delete))
        {
            // todo: this is only matching by key, but non-tree based serializers also delete by alias.
            // so this check actually has to be booted back down to the serializer.
            if (actions.Any(x => x.Change != ChangeType.Delete && DoActionsMatch(x, deleteAction)))
            {
                var duplicateAction = uSyncActionHelper<TObject>.ReportActionFail(deleteAction.Name,
                    $"Duplicate! {deleteAction.Name} exists both as delete and import action");

                // create a detail message to tell people what has happened.
                duplicateAction.DetailMessage = "uSync detected a duplicate actions, where am item will be both created and deleted.";
                var details = new List<uSyncChange>();

                // add the delete message to the list of changes
                var filename = Path.GetFileName(deleteAction.FileName) ?? string.Empty;
                var relativePath = deleteAction.FileName?.Substring(folder.Length) ?? string.Empty;

                details.Add(uSyncChange.Delete(filename, $"Delete: {deleteAction.Name} ({filename}", relativePath));

                // add all the duplicates to the list of changes.
                foreach (var dup in actions.Where(x => x.Change != ChangeType.Delete && DoActionsMatch(x, deleteAction)))
                {
                    var dupFilename = Path.GetFileName(dup.FileName) ?? string.Empty;
                    var dupRelativePath = dup.FileName?.Substring(folder.Length) ?? string.Empty;

                    details.Add(
                        uSyncChange.Update(
                            path: dupFilename,
                            name: $"{dup.Change} : {dup.Name} ({dupFilename})",
                            oldValue: "",
                            newValue: dupRelativePath));
                }

                duplicateAction.Details = details;
                duplicates.Add(duplicateAction);
            }
        }

        return duplicates;
    }


    /// <summary>
    ///  check to see if an action matches, another action. 
    /// </summary>
    /// <remarks>
    ///  how two actions match can vary based on handler, in the most part they are matched by key
    ///  but some items will also check based on the name.
    ///  
    ///  when we are dealing with handlers where things can have the same 
    ///  name (tree items, such as content or media), this function has 
    ///  to be overridden to remove the name check.
    /// </remarks>

    protected virtual bool DoActionsMatch(uSyncAction a, uSyncAction b)
    {
        if (a.Key == b.Key) return true;
        if (a.Name.Equals(b.Name, StringComparison.InvariantCultureIgnoreCase)) return true;
        return false;
    }

    /// <summary>
    ///  check if a node matches a item 
    /// </summary>
    /// <remarks>
    ///  Like above we want to match on key and alias, but only if the alias is unique. 
    ///  however the GetItemAlias function is overridden by tree based handlers to return a unique 
    ///  alias (the key again), so we don't get false positives. 
    /// </remarks>
    protected virtual bool DoItemsMatch(XElement node, TObject item)
    {
        if (GetItemKey(item) == node.GetKey()) return true;

        // yes this is an or, we've done it explicitly, so you can tell!
        if (node.GetAlias().Equals(this.GetItemAlias(item), StringComparison.InvariantCultureIgnoreCase)) return true;

        return false;
    }

    /// <summary>
    ///  Check report for any items that are missing their parent items 
    /// </summary>
    /// <remarks>
    ///  The serializers will report if an item is missing a parent item within umbraco,
    ///  but because the serializer isn't aware of the wider import (all the other items)
    ///  it can't say if the parent is in the import.
    ///  
    ///  This method checks for the parent of an item in the wider list of items being 
    ///  imported.
    /// </remarks>
    private List<uSyncAction> ReportMissingParents(uSyncAction[] actions)
    {
        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i].Change != ChangeType.ParentMissing || actions[i].FileName is null) continue;

            var node = XElement.Load(actions[i].FileName!);
            var guid = node.GetParentKey();

            if (guid != Guid.Empty)
            {
                if (actions.Any(x => x.Key == guid && (x.Change < ChangeType.Fail || x.Change == ChangeType.ParentMissing)))
                {
                    logger.LogDebug("Found existing key in actions {item}", actions[i].Name);
                    actions[i].Change = ChangeType.Create;
                }
                else
                {
                    logger.LogWarning("{item} is missing a parent", actions[i].Name);
                }
            }
        }

        return [.. actions];
    }

    /// <summary>
    ///  Run a report on a given folder
    /// </summary>
    protected virtual async Task<IEnumerable<uSyncAction>> ReportFolderAsync(string folder, HandlerSettings config, SyncUpdateCallback? callback)
    {
        List<uSyncAction> actions = [];

        var files = GetImportFiles(folder).ToList();

        int count = 0;

        logger.LogDebug("ReportFolder: {folder} ({count} files)", folder, files.Count);

        foreach (string file in files)
        {
            count++;
            callback?.Invoke(Path.GetFileNameWithoutExtension(file), count, files.Count);

            actions.AddRange(await ReportItemAsync(file, config));
        }

        foreach (var children in syncFileService.GetDirectories(folder))
        {
            actions.AddRange(await ReportFolderAsync(children, config, callback));
        }

        return actions;
    }

    /// <summary>
    /// Report on any changes for a single XML node.
    /// </summary>
    [Obsolete("Use ReportElementAsync will be removed in v16")]
    protected virtual IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings? config)
        => ReportElementAsync(node, filename, config ?? this.DefaultConfig, new uSyncImportOptions()).Result;

    public virtual async Task<IEnumerable<uSyncAction>> ReportElementAsync(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options)
    {
        try
        {
            //  starting reporting notification
            //  this lets us intercept a report and 
            //  shortcut the checking (sometimes).
            if (await _mutexService.FireItemStartingEventAsync(new uSyncReportingItemNotification(node)))
            {
                return uSyncActionHelper<TObject>
                    .ReportAction(ChangeType.NoChange, node.GetAlias(), node.GetPath(), GetNameFromFileOrNode(filename, node), node.GetKey(), this.Alias,
                        "Change stopped by delegate event")
                    .AsEnumerableOfOne();
            }

            var actions = new List<uSyncAction>();

            // get the serializer options
            var serializerOptions = new SyncSerializerOptions(options.Flags, settings.Settings, options.UserId);
            serializerOptions.MergeSettings(options.Settings);

            // check if this item is current (the provided XML and exported XML match)
            var change = await IsItemCurrentAsync(node, serializerOptions);

            var action = uSyncActionHelper<TObject>
                    .ReportAction(change.Change, node.GetAlias(), node.GetPath(), GetNameFromFileOrNode(filename, node), node.GetKey(), this.Alias, "");



            action.Message = "";

            if (action.Change == ChangeType.Clean)
            {
                actions.AddRange(await CleanFolderAsync(filename, true, settings.UseFlatStructure));
            }
            else if (action.Change > ChangeType.NoChange)
            {
                if (change.CurrentNode is not null)
                {
                    action.Details = await GetChangesAsync(node, change.CurrentNode, serializerOptions);
                    if (action.Change != ChangeType.Create && (action.Details == null || !action.Details.Any()))
                    {
                        action.Message = "XML is different - but properties may not have changed";
                        action.Details = SyncHandlerRoot<TObject, TContainer>.MakeRawChange(node, change.CurrentNode).AsEnumerableOfOne();
                    }
                    else
                    {
                        action.Message = $"{action.Change}";
                    }
                }
                actions.Add(action);
            }
            else
            {
                actions.Add(action);
            }

            // tell other things we have reported this item.
            await _mutexService.FireItemCompletedEventAsync(new uSyncReportedItemNotification(node, action.Change));

            return actions;
        }
        catch (FormatException fex)
        {
            return uSyncActionHelper<TObject>
                .ReportActionFail(Path.GetFileName(node.GetAlias()), $"format error {fex.Message}")
                .AsEnumerableOfOne();
        }
    }

    private static uSyncChange MakeRawChange(XElement node, XElement current)
    {
        if (current != null)
            return uSyncChange.Update(node.GetAlias(), "Raw XML", current.ToString(), node.ToString());

        return uSyncChange.NoChange(node.GetAlias(), node.GetAlias());
    }

    /// <summary>
    /// Run a report on a single file.
    /// </summary>
    [Obsolete("Use ReportItemAsync will be removed in v16")]
    protected IEnumerable<uSyncAction> ReportItem(string file, HandlerSettings config)
        => ReportItemAsync(file, config).Result;
    protected async Task<IEnumerable<uSyncAction>> ReportItemAsync(string file, HandlerSettings config)
    {
        try
        {
            var node = await syncFileService.LoadXElementAsync(file);

            if (await ShouldImportAsync(node, config))
            {
                return await ReportElementAsync(node, file, config, new uSyncImportOptions());
            }
            else
            {
                return uSyncActionHelper<TObject>.ReportAction(ChangeType.NoChange, node.GetAlias(), node.GetPath(), syncFileService.GetSiteRelativePath(file), node.GetKey(),
                    this.Alias, "Will not be imported (Based on configuration)")
                    .AsEnumerableOfOne<uSyncAction>();
            }
        }
        catch (Exception ex)
        {
            return uSyncActionHelper<TObject>
                .ReportActionFail(Path.GetFileName(file), $"Reporting error {ex.Message}")
                .AsEnumerableOfOne();
        }

    }


    private async Task<IEnumerable<uSyncChange>> GetChangesAsync(XElement node, XElement currentNode, SyncSerializerOptions options)
        => await itemFactory.GetChangesAsync<TObject>(node, currentNode, options);

    #endregion

    #region Notification Events 

    /// <summary>
    /// calculate if this handler should process the events.  
    /// </summary>
    /// <remarks>
    /// will check if uSync is paused, the handler is enabled or the action is set.
    /// </remarks>
    protected bool ShouldProcessEvent()
    {
        if (_mutexService.IsPaused) return false;
        if (!DefaultConfig.Enabled) return false;


        var group = !string.IsNullOrWhiteSpace(DefaultConfig.Group) ? DefaultConfig.Group : this.Group;

        if (uSyncConfig.Settings.ExportOnSave.InvariantContains("All") ||
            uSyncConfig.Settings.ExportOnSave.InvariantContains(group))
        {
            return HandlerActions.Save.IsValidAction(DefaultConfig.Actions);
        }

        return false;
    }

    /// <summary>
    /// Handle an Umbraco Delete notification
    /// </summary>
    public virtual async Task HandleAsync(DeletedNotification<TObject> notification, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent()) return;

        foreach (var item in notification.DeletedEntities)
        {
            try
            {
                var handlerFolders = GetDefaultHandlerFolders();
                await ExportDeletedItemAsync(item, handlerFolders, DefaultConfig);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create delete marker");
                notification.Messages.Add(new EventMessage("uSync", $"Failed to mark as deleted : {ex.Message}", EventMessageType.Warning));
            }
        }
    }

    /// <summary>
    /// Handle the Umbraco Saved notification for items. 
    /// </summary>
    /// <param name="notification"></param>
    public virtual async Task HandleAsync(SavedNotification<TObject> notification, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent()) return;
        if (notification.State.TryGetValue(uSync.EventPausedKey, out var paused) && paused is true)
            return;

        var handlerFolders = GetDefaultHandlerFolders();

        foreach (var item in notification.SavedEntities)
        {
            try
            {
                var attempts = await ExportAsync(item, handlerFolders, DefaultConfig);
                foreach (var attempt in attempts.Where(x => x.Success))
                {
                    if (attempt.FileName is null) continue;
                    await this.CleanUpAsync(item, attempt.FileName, handlerFolders.Last());
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create uSync export file");
                notification.Messages.Add(new EventMessage("uSync", $"Failed to create export file : {ex.Message}", EventMessageType.Warning));
            }
        }
    }

    /// <summary>
    /// Handle the Umbraco moved notification for items.
    /// </summary>
    /// <param name="notification"></param>
    public virtual async Task HandleAsync(MovedNotification<TObject> notification, CancellationToken cancellationToken)
    {
        try
        {
            if (!ShouldProcessEvent()) return;
            await HandleMoveAsync(notification.MoveInfoCollection, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to export move operation");
            notification.Messages.Add(new EventMessage("uSync", $"Failed to export move : {ex.Message}", EventMessageType.Warning));
        }

    }

    /// <summary>
    /// Process a collection of move events 
    /// </summary>
    /// <remarks>
    /// This has been separated out, because we also call this code when a handler supports
    /// recycle bin events 
    /// </remarks>
    protected async Task HandleMoveAsync(IEnumerable<MoveEventInfoBase<TObject>> moveInfoCollection, CancellationToken cancellationToken)
    {
        foreach (var item in moveInfoCollection)
        {
            var handlerFolders = GetDefaultHandlerFolders();
            var attempts = await ExportAsync(item.Entity, handlerFolders, DefaultConfig);

            if (!this.DefaultConfig.UseFlatStructure)
            {
                // moves only need cleaning up if we are not using flat, because 
                // with flat the file will always be in the same folder.

                foreach (var attempt in attempts.Where(x => x.Success is true))
                {
                    if (attempt.FileName is null) continue;
                    await this.CleanUpAsync(item.Entity, attempt.FileName, handlerFolders.Last());
                }
            }
        }
    }

    protected virtual async Task ExportDeletedItemAsync(TObject item, string[] folders, HandlerSettings config)
    {
        if (item == null) return;

        var targetFolder = folders.Last();

        var filename = (await GetPathAsync(targetFolder, item, config.GuidNames, config.UseFlatStructure))
            .ToAppSafeFileName();

        if (IsLockedAtRoot(folders, filename.Substring(targetFolder.Length + 1)))
        {
            // don't do anything this thing exists at a higher level. ! 
            return;
        }

        if (await ShouldExportDeletedFileAsync(item, config) is false) return;

        var attempt = await serializer.SerializeEmptyAsync(item, SyncActionType.Delete, string.Empty);
        if (attempt.Item is not null && await ShouldExportAsync(attempt.Item, config) is true)
        {
            if (attempt.Success && attempt.Change != ChangeType.NoChange)
            {
                await syncFileService.SaveXElementAsync(attempt.Item, filename);

                // so check - it shouldn't (under normal operation) 
                // be possible for a clash to exist at delete, because nothing else 
                // will have changed (like name or location) 

                // we only then do this if we are not using flat structure. 
                if (!DefaultConfig.UseFlatStructure)
                    await this.CleanUpAsync(item, filename, Path.Combine(folders.Last(), this.DefaultFolder));
            }
        }
    }

    private async Task<bool> ShouldExportDeletedFileAsync(TObject item, HandlerSettings config)
    {
        try
        {
            var deletingAttempt = await serializer.SerializeAsync(item, new SyncSerializerOptions(config.Settings));
            if (deletingAttempt.Item is null) return true;
            return await ShouldExportAsync(deletingAttempt.Item, config);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to calculate if this item should be exported when deleted, the common option is yes, so we will");
            return true;
        }
    }

    /// <summary>
    ///  get all the possible folders for this handlers 
    /// </summary>
    protected string[] GetDefaultHandlerFolders()
        => RootFolders.Select(f => Path.Combine(f, DefaultFolder)).ToArray();


    /// <summary>
    ///  Cleans up the handler folder, removing duplicate files for this item
    ///  </summary>
    ///  <remarks>
    ///   e.g if someone renames a thing (and we are using the name in the file) 
    ///   this will clean anything else in the folder that has that key / alias
    ///  </remarks>
    [Obsolete("Use CleanUpAsync will be removed in v16")]
    protected virtual void CleanUp(TObject item, string newFile, string folder)
        => CleanUpAsync(item, newFile, folder).Wait();

    protected virtual async Task CleanUpAsync(TObject item, string newFile, string folder)
    {
        var physicalFile = syncFileService.GetAbsPath(newFile);

        var files = syncFileService.GetFiles(folder, $"*.{this.uSyncConfig.Settings.DefaultExtension}");

        foreach (string file in files)
        {
            // compare the file paths. 
            if (!syncFileService.PathMatches(physicalFile, file)) // This is not the same file, as we are saving.
            {
                try
                {
                    var node = await syncFileService.LoadXElementAsync(file);

                    // if this XML file matches the item we have just saved. 

                    if (!node.IsEmptyItem() || node.GetEmptyAction() != SyncActionType.Rename)
                    {
                        // the node isn't empty, or its not a rename (because all clashes become renames)

                        if (DoItemsMatch(node, item))
                        {
                            logger.LogDebug("Duplicate {file} of {alias}, saving as rename", Path.GetFileName(file), this.GetItemAlias(item));

                            var attempt = await serializer.SerializeEmptyAsync(item, SyncActionType.Rename, node.GetAlias());
                            if (attempt.Success && attempt.Item is not null)
                            {
                                await syncFileService.SaveXElementAsync(attempt.Item, file);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Error during cleanup of existing files {message}", ex.Message);
                    // cleanup should fail silently ? - because it can impact on normal Umbraco operations?
                }
            }
        }

        var folders = syncFileService.GetDirectories(folder);
        foreach (var children in folders)
        {
            await CleanUpAsync(item, newFile, children);
        }
    }

    #endregion

    // 98% of the time the serializer can do all these calls for us, 
    // but for blueprints, we want to get different items, (but still use the 
    // content serializer) so we override them.

    /// <summary>
    /// Fetch an item via the Serializer
    /// </summary>
    [Obsolete("This method is not used and will be removed in v16")]
    protected virtual TObject? GetFromService(Guid key) => GetFromServiceAsync(key).Result;

    protected virtual async Task<TObject?> GetFromServiceAsync(Guid key) => await serializer.FindItemAsync(key);

    /// <summary>
    /// Fetch an item via the Serializer
    /// </summary>
    [Obsolete("This method is not used and will be removed in v16")]
    protected virtual TObject? GetFromService(string alias) => GetFromServiceAsync(alias).Result;
    protected virtual async Task<TObject?> GetFromServiceAsync(string alias) => await serializer.FindItemAsync(alias);

    /// <summary>
    /// Delete an item via the Serializer
    /// </summary>
    [Obsolete("This method is not used and will be removed in v16")]
    protected virtual void DeleteViaService(TObject item) => DeleteViaServiceAsync(item).Wait();
    protected virtual async Task DeleteViaServiceAsync(TObject item) => await serializer.DeleteItemAsync(item);

    /// <summary>
    /// Get the alias of an item from the Serializer
    /// </summary>
    protected string GetItemAlias(TObject item) => serializer.ItemAlias(item);

    /// <summary>
    /// Get the Key of an item from the Serializer
    /// </summary>
    protected Guid GetItemKey(TObject item) => serializer.ItemKey(item);

    /// <summary>
    /// Get a container item from the Umbraco service.
    /// </summary>

    [Obsolete("This method is not used and will be removed in v16")]
    virtual protected TObject? GetFromService(TContainer? item)
        => GetFromServiceAsync(item).Result;

    abstract protected Task<TObject?> GetFromServiceAsync(TContainer? item);

    /// <summary>
    /// Get a container item from the Umbraco service.
    /// </summary>
    [Obsolete("This method is not used and will be removed in v16")]
    virtual protected TContainer? GetContainer(Guid key) => default;

    virtual protected Task<TContainer?> GetContainerAsync(Guid key) => Task.FromResult<TContainer?>(default);

    /// <summary>
    /// Get a container item from the Umbraco service.
    /// </summary>
    [Obsolete("This method is not used and will be removed in v16")]
    virtual protected TContainer? GetContainer(int id) => default;

    /// <summary>
    /// Get the file path to use for an item
    /// </summary>
    /// <param name="item">Item to derive path for</param>
    /// <param name="useGuid">should we use the key value in the path</param>
    /// <param name="isFlat">should the file be flat and ignore any sub folders?</param>
    /// <returns>relative file path to use for an item</returns>
    virtual protected string GetItemPath(TObject item, bool useGuid, bool isFlat)
        => useGuid ? GetItemKey(item).ToString() : GetItemFileName(item);

    /// <summary>
    /// Get the file name to use for an item
    /// </summary>
    virtual protected string GetItemFileName(TObject item)
        => GetItemAlias(item).ToSafeFileName(shortStringHelper);

    /// <summary>
    /// Get the name of a supplied item
    /// </summary>
    abstract protected string GetItemName(TObject item);

    /// <summary>
    /// Calculate the relative Physical path value for any item 
    /// </summary>
    /// <remarks>
    /// this is where a item is saved on disk in relation to the uSync folder 
    /// </remarks>
    virtual protected async Task<string> GetPathAsync(string folder, TObject item, bool GuidNames, bool isFlat)
    {
        if (isFlat && GuidNames) return Path.Combine(folder, $"{GetItemKey(item)}.{this.uSyncConfig.Settings.DefaultExtension}");
        var path = Path.Combine(folder, $"{this.GetItemPath(item, GuidNames, isFlat)}.{this.uSyncConfig.Settings.DefaultExtension}");

        // if this is flat but not using GUID filenames, then we check for clashes.
        if (isFlat && !GuidNames) return await CheckAndFixFileClashAsync(path, item);
        return path;
    }


    /// <summary>
    ///  Get a clean filename that doesn't clash with any existing items.
    /// </summary>
    /// <remarks>
    ///  clashes we want to resolve can occur when the safeFilename for an item
    ///  matches with the safe file name for something else. e.g
    ///     1 Special Doc-type 
    ///     2 Special Doc-type 
    ///     
    ///  Will both resolve to SpecialDocType.Config
    ///  
    ///  the first item to be written to disk for a clash will get the 'normal' name
    ///  all subsequent items will get the appended name. 
    ///  
    ///  this can be completely sidestepped by using GUID filenames. 
    /// </remarks>
    virtual protected async Task<string> CheckAndFixFileClashAsync(string path, TObject item)
    {
        if (syncFileService.FileExists(path))
        {
            var node = await syncFileService.LoadXElementAsync(path);

            if (node == null) return path;
            if (GetItemKey(item) == node.GetKey()) return path;
            if (GetXmlMatchString(node) == GetItemMatchString(item)) return path;

            // get here we have a clash, we should append something
            var append = GetItemKey(item).ToShortKeyString(8); // (this is the shortened GUID like media folders do)
            return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                Path.GetFileNameWithoutExtension(path) + "_" + append + Path.GetExtension(path));
        }

        return path;
    }

    /// <summary>
    ///  a string we use to match this item, with other (where there are levels)
    /// </summary>
    /// <remarks>
    ///  this works because unless it's content/media you can't actually have 
    ///  clashing aliases at different levels in the folder structure. 
    ///  
    ///  So just checking the alias works, for content we overwrite these two functions.
    /// </remarks>
    protected virtual string GetItemMatchString(TObject item) => GetItemAlias(item);

    /// <summary>
    ///  Calculate the matching item string from the loaded uSync XML element 
    /// </summary>
    protected virtual string GetXmlMatchString(XElement node) => node.GetAlias();

    /// <summary>
    /// Rename an item 
    /// </summary>
    /// <remarks>
    ///  This doesn't get called, because renames generally are handled in the serialization because we match by key.
    /// </remarks>
    virtual public uSyncAction Rename(TObject item) => new();

    /// <summary>
    ///  Group a handler belongs too (default will be settings)
    /// </summary>
    public virtual string Group { get; protected set; } = uSyncConstants.Groups.Settings;

    /// <summary>
    /// Serialize an item to XML based on a given UDI value
    /// </summary>
    public async Task<SyncAttempt<XElement>> GetElementAsync(Udi udi)
    {
        var element = await FindByUdiAsync(udi);
        if (element != null)
            return await SerializeItemAsync(element, new SyncSerializerOptions());

        return SyncAttempt<XElement>.Fail(udi.ToString(), ChangeType.Fail, "Item not found");
    }


    private async Task<TObject?> FindByUdiAsync(Udi udi)
    {
        return udi switch
        {
            GuidUdi guidUdi => await GetFromServiceAsync(guidUdi.Guid),
            StringUdi stringUdi => await GetFromServiceAsync(stringUdi.Id),
            _ => default,
        };
    }

    /// <summary>
    /// Calculate any dependencies for any given item based on loaded dependency checkers 
    /// </summary>
    /// <remarks>
    /// uSync contains no dependency checkers by default - uSync.Complete will load checkers
    /// when installed. 
    /// </remarks>
    public async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(Guid key, DependencyFlags flags)
    {
        if (key == Guid.Empty)
        {
            return await GetContainerDependenciesAsync(default, flags);
        }
        else
        {
            var item = await this.GetFromServiceAsync(key);
            if (item == null)
            {
                var container = await this.GetContainerAsync(key);
                if (container != null)
                {
                    return await GetContainerDependenciesAsync(container, flags);
                }
                return [];
            }

            return await GetDependenciesAsync(item, flags);
        }
    }

    private bool HasDependencyCheckers()
        => dependencyCheckers != null && dependencyCheckers.Count > 0;


    /// <summary>
    /// Calculate any dependencies for any given item based on loaded dependency checkers 
    /// </summary>
    /// <remarks>
    /// uSync contains no dependency checkers by default - uSync.Complete will load checkers
    /// when installed. 
    /// </remarks>
    [Obsolete("Use GetDependenciesAsync will be removed in v16")]
    protected IEnumerable<uSyncDependency> GetDependencies(TObject item, DependencyFlags flags)
        => GetDependenciesAsync(item, flags).Result;

    protected async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(TObject item, DependencyFlags flags)
    {
        if (item == null || !HasDependencyCheckers()) return [];

        var dependencies = new List<uSyncDependency>();
        foreach (var checker in dependencyCheckers)
        {
            dependencies.AddRange(await checker.GetDependenciesAsync(item, flags));
        }
        return dependencies;
    }

    /// <summary>
    /// Calculate any dependencies for any given item based on loaded dependency checkers 
    /// </summary>
    /// <remarks>
    /// uSync contains no dependency checkers by default - uSync.Complete will load checkers
    /// when installed. 
    /// </remarks>
    private async Task<IEnumerable<uSyncDependency>> GetContainerDependenciesAsync(TContainer? parent, DependencyFlags flags)
    {
        if (!HasDependencyCheckers()) return [];

        var dependencies = new List<uSyncDependency>();

        var containers = await GetFoldersAsync(parent);
        if (containers != null && containers.Any())
        {
            foreach (var container in containers)
            {
                dependencies.AddRange(await GetContainerDependenciesAsync(container, flags));
            }
        }

        var children = await GetChildItemsAsync(parent);
        if (children != null && children.Any())
        {
            foreach (var child in children)
            {
                var childItem = await GetFromServiceAsync(child);
                if (childItem != null)
                {
                    foreach (var checker in dependencyCheckers)
                    {
                        dependencies.AddRange(await checker.GetDependenciesAsync(childItem, flags));
                    }
                }
            }
        }

        return dependencies.DistinctBy(x => x.Udi?.ToString() ?? x.Name).OrderByDescending(x => x.Order);
    }

    #region Serializer Calls 

    /// <summary>
    ///  call the serializer to get an items xml.
    /// </summary>
    [Obsolete("Use SerializeItemAsync instead will be removed in v16")]
    protected SyncAttempt<XElement> SerializeItem(TObject item, SyncSerializerOptions options)
        => SerializeItemAsync(item, options).Result;

    protected async Task<SyncAttempt<XElement>> SerializeItemAsync(TObject item, SyncSerializerOptions options)
        => await serializer.SerializeAsync(item, options);

    /// <inheritdoc />
    /// <summary>
    ///  turn the xml into an item (and optionally save it to umbraco).
    /// </summary>
    [Obsolete("Use DeserializeItemAsync instead will be removed in v16")]
    protected SyncAttempt<TObject> DeserializeItem(XElement node, SyncSerializerOptions options)
        => DeserializeItemAsync(node, options).Result;
    protected async Task<SyncAttempt<TObject>> DeserializeItemAsync(XElement node, SyncSerializerOptions options)
            => await serializer.DeserializeAsync(node, options);

    /// <summary>
    ///  perform a second pass on an item you are importing.
    /// </summary>
    [Obsolete("use DeserializeItemSecondPassAsync instead will be removed in v16")]
    protected SyncAttempt<TObject> DeserializeItemSecondPass(TObject item, XElement node, SyncSerializerOptions options)
        => DeserializeItemSecondPassAsync(item, node, options).Result;
    protected async Task<SyncAttempt<TObject>> DeserializeItemSecondPassAsync(TObject item, XElement node, SyncSerializerOptions options)
        => await serializer.DeserializeSecondPassAsync(item, node, options);

    [Obsolete("use IsItemCurrentAsync instead will be removed in v16")]
    private SyncChangeInfo IsItemCurrent(XElement node, SyncSerializerOptions options)
        => IsItemCurrentAsync(node, options).Result;

    private async Task<SyncChangeInfo> IsItemCurrentAsync(XElement node, SyncSerializerOptions options)
    {
        var change = new SyncChangeInfo
        {
            CurrentNode = await SerializeFromNodeAsync(node, options),
        };
        change.Change = await serializer.IsCurrentAsync(node, change.CurrentNode, options);
        return change;
    }

    private async Task<XElement?> SerializeFromNodeAsync(XElement node, SyncSerializerOptions options)
    {
        var item = await serializer.FindItemAsync(node);
        if (item != null)
        {
            var cultures = node.GetCultures();
            if (!string.IsNullOrWhiteSpace(cultures))
            {
                // the cultures we serialize should match any in the file.
                // this means we then only check the same values at each end.
                options.Settings[Core.uSyncConstants.CultureKey] = cultures;
            }

            var attempt = await this.SerializeItemAsync(item, options);
            if (attempt.Success) return attempt.Item;
        }

        return null;
    }

    private class SyncChangeInfo
    {
        public ChangeType Change { get; set; }
        public XElement? CurrentNode { get; set; }
    }

    public async Task<Udi?> FindFromNodeAsync(XElement node)
    {
        var item = await serializer.FindItemAsync(node);
        if (item is null) return null;
        return Udi.Create(this.EntityType, serializer.ItemKey(item));
    }

    /// <summary>
    /// Calculate the current status of an item compared to the XML in a potential import
    /// </summary>
    [Obsolete("use IsItemCurrentAsync instead will be removed in v16")]
    public ChangeType GetItemStatus(XElement node)
    {
        var serializerOptions = new SyncSerializerOptions(SerializerFlags.None, this.DefaultConfig.Settings);
        return this.IsItemCurrent(node, serializerOptions).Change;
    }

    public async Task<ChangeType> GetItemStatusAsync(XElement node)
    {
        var options = new SyncSerializerOptions(SerializerFlags.None, this.DefaultConfig.Settings);
        return (await this.IsItemCurrentAsync(node, options)).Change;
    }

    #endregion

    private string GetNameFromFileOrNode(string filename, XElement node)
    {
        if (string.IsNullOrWhiteSpace(filename) is true) return node.GetAlias();
        return syncFileService.GetSiteRelativePath(filename);
    }


    /// <summary>
    ///  get the key for any caches we might call (thread based cache value)
    /// </summary>
    /// <returns></returns>
    protected string GetCacheKeyBase()
        => $"keyCache_{this.Alias}_{Environment.CurrentManagedThreadId}";

    private string PrepCaches()
    {
        if (this.serializer is ISyncCachedSerializer cachedSerializer)
            cachedSerializer.InitializeCache();

        // make sure the runtime cache is clean.
        var key = GetCacheKeyBase();

        // this also clears the folder cache - as its a starts with call.
        runtimeCache.ClearByKey(key);
        return key;
    }

    private void CleanCaches(string cacheKey)
    {
        runtimeCache.ClearByKey(cacheKey);

        if (this.serializer is ISyncCachedSerializer cachedSerializer)
            cachedSerializer.DisposeCache();

    }

    #region roots notifications 

    /// <summary>
    ///  check roots isn't blocking the save
    /// </summary>
    public virtual async Task HandleAsync(SavingNotification<TObject> notification, CancellationToken cancellationToken)
    {
        if (await ShouldBlockRootChangesAsync(notification.SavedEntities))
        {
            notification.Cancel = true;
            notification.Messages.Add(GetCancelMessageForRoots());
        }
    }

    /// <summary>
    ///  check roots isn't blocking the move
    /// </summary>
    public virtual async Task HandleAsync(MovingNotification<TObject> notification, CancellationToken cancellationToken)
    {
        if (await ShouldBlockRootChangesAsync(notification.MoveInfoCollection.Select(x => x.Entity)))
        {
            notification.Cancel = true;
            notification.Messages.Add(GetCancelMessageForRoots());
        }
    }

    /// <summary>
    ///  check roots isn't blocking the delete
    /// </summary>
    public virtual async Task HandleAsync(DeletingNotification<TObject> notification, CancellationToken cancellationToken)
    {
        if (await ShouldBlockRootChangesAsync(notification.DeletedEntities))
        {
            notification.Cancel = true;
            notification.Messages.Add(GetCancelMessageForRoots());
        }
    }

    /// <summary>
    ///  should we block this event based on the existence or root objects.
    /// </summary>
    protected async Task<bool> ShouldBlockRootChangesAsync(IEnumerable<TObject> items)
    {
        if (!ShouldProcessEvent()) return false;

        if (uSyncConfig.Settings.LockRoot == false) return false;

        if (!HasRootFolders()) return false;

        foreach (var item in items)
        {
            if (await RootItemExistsAsync(item))
                return true;
        }

        return false;
    }

    /// <summary>
    ///  get the message we use for cancellations
    /// </summary>
    protected EventMessage GetCancelMessageForRoots()
        => new("Blocked", "You cannot make this change, root level items are locked", EventMessageType.Error);


    private bool HasRootFolders()
        => syncFileService.AnyFolderExists(uSyncConfig.GetFolders()[..^1]);

    private async Task<bool> RootItemExistsAsync(TObject item)
    {
        foreach (var folder in uSyncConfig.GetFolders()[..^1])
        {
            var filename = (await GetPathAsync(
                Path.Combine(folder, DefaultFolder),
                item,
                DefaultConfig.GuidNames,
                DefaultConfig.UseFlatStructure))
                .ToAppSafeFileName();

            if (syncFileService.FileExists(filename))
                return true;

        }

        return false;
    }

    #endregion
}