using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

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
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.BackOffice.SyncHandlers
{
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
        protected readonly SyncFileService syncFileService;

        /// <summary>
        /// Reference to the Event service used to handle locking
        /// </summary>
        protected readonly uSyncEventService _mutexService;

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
        ///  the root folder for the handler (based on the settings)
        /// </summary>
        [Obsolete("we should be using the array of folders, will be removed in v15")]
        protected string rootFolder { get; set; }

        /// <summary>
        ///  the root folders to use for the handler (based on settings).
        /// </summary>
        protected string[] rootFolders { get; set; }

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
        protected UmbracoObjectTypes itemObjectType { get; set; } = UmbracoObjectTypes.Unknown;

        /// <summary>
        /// UmbracoObjectType of containers manged by this handler
        /// </summary>
        protected UmbracoObjectTypes itemContainerType = UmbracoObjectTypes.Unknown;

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
                SyncFileService syncFileService,
                uSyncEventService mutexService,
                uSyncConfigService uSyncConfig,
                ISyncItemFactory itemFactory)
        {
            this.uSyncConfig = uSyncConfig;

            this.logger = logger;
            this.shortStringHelper = shortStringHelper;
            this.itemFactory = itemFactory;

            this.serializer = this.itemFactory.GetSerializers<TObject>().FirstOrDefault();
            this.trackers = this.itemFactory.GetTrackers<TObject>().ToList();
            this.dependencyCheckers = this.itemFactory.GetCheckers<TObject>().ToList();

            this.syncFileService = syncFileService;
            this._mutexService = mutexService;

            var currentHandlerType = GetType();
            var meta = currentHandlerType.GetCustomAttribute<SyncHandlerAttribute>(false);
            if (meta == null)
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

            this.itemObjectType = uSyncObjectType.ToUmbracoObjectType(EntityType);
            this.itemContainerType = uSyncObjectType.ToContainerUmbracoObjectType(EntityType);

            GetDefaultConfig();

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

        private void GetDefaultConfig()
        {
            var defaultSet = uSyncConfig.GetDefaultSetSettings();
            this.DefaultConfig = defaultSet.GetHandlerSettings(this.Alias);

            if (defaultSet.DisabledHandlers.InvariantContains(this.Alias))
                this.DefaultConfig.Enabled = false;

            rootFolder = uSyncConfig.GetRootFolder();

            rootFolders = uSyncConfig.GetFolders();
        }

        #region Importing 

        /// <summary>
        ///  Import everything from a given folder, using the supplied configuration settings.
        /// </summary>
        public IEnumerable<uSyncAction> ImportAll(string folder, HandlerSettings config, bool force, SyncUpdateCallback callback = null)
            => ImportAll([folder], config, new uSyncImportOptions
            {
                Flags = force ? SerializerFlags.Force : SerializerFlags.None,
                Callbacks = new uSyncCallbacks(null, callback)
            });

        /// <summary>
        ///  import everything from a collection of folders, using the supplied config.
        /// </summary>
        /// <remarks>
        ///  allows us to 'merge' a collection of folders down and perform an import against them (without first having to actually merge the folders on disk)
        /// </remarks>
        public IEnumerable<uSyncAction> ImportAll(string[] folders, HandlerSettings config, uSyncImportOptions options)
        {
            var cacheKey = PrepCaches();
            runtimeCache.ClearByKey(cacheKey);

            var items = GetMergedItems(folders);

            // create the update list with items.count space. this is the max size we need this list. 
            List<uSyncAction> actions = new List<uSyncAction>(items.Count);
            List<ImportedItem<TObject>> updates = new List<ImportedItem<TObject>>(items.Count);
            List<string> cleanMarkers = [];

            int count = 0;
            int total = items.Count;

            foreach (var item in items)
            {
                count++;

                options.Callbacks?.Update?.Invoke($"Importing {Path.GetFileNameWithoutExtension(item.Path)}", count, total);

                var result = ImportElement(item.Node, item.Path, config, options);
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
                            updates.Add(new ImportedItem<TObject>(item.Node,update));
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
                    serializer.Save(updates.Select(x => x.Item));
                }

                PerformSecondPassImports(updates, actions, config, options.Callbacks?.Update);
            }

            if (actions.All(x => x.Success) && cleanMarkers.Count > 0)
            {
                PerformImportClean(cleanMarkers, actions, config, options.Callbacks?.Update);
            }

            CleanCaches(cacheKey);
            options.Callbacks.Update?.Invoke("Done", 3, 3);

            return actions;
        }

        /// <summary>
        ///  get all items for the report/import process.
        /// </summary>
        /// <param name="folders"></param>
        /// <returns></returns>
        public IReadOnlyList<OrderedNodeInfo> FetchAllNodes(string[] folders)
            => GetMergedItems(folders);

        /// <summary>
        ///  method to get the merged folders, handlers that care about orders should override this. 
        /// </summary>
        protected virtual IReadOnlyList<OrderedNodeInfo> GetMergedItems(string[] folders)
        {
            var baseTracker = trackers.FirstOrDefault() as ISyncTrackerBase;
            return syncFileService.MergeFolders(folders, uSyncConfig.Settings.DefaultExtension, baseTracker).ToArray();
        }

        private void PerformImportClean(List<string> cleanMarkers, List<uSyncAction> actions, HandlerSettings config, SyncUpdateCallback callback)
        {
            foreach (var item in cleanMarkers.Select((filePath, Index) => new { filePath, Index }))
            {
                var folderName = Path.GetFileName(item.filePath);
                callback?.Invoke($"Cleaning {folderName}", item.Index, cleanMarkers.Count);

                var cleanActions = CleanFolder(item.filePath, false, config.UseFlatStructure);
                if (cleanActions.Any())
                {
                    actions.AddRange(cleanActions);
                }
                else
                {
                    // nothing to delete, we report this as a no change 
                    actions.Add(uSyncAction.SetAction(true, $"Folder {Path.GetFileName(item.filePath)}", change: ChangeType.NoChange, filename: item.filePath));
                }
            }
            // remove the actual cleans (they will have been replaced by the deletes
            actions.RemoveAll(x => x.Change == ChangeType.Clean);
        }

        /// <summary>
        ///  Import everything in a given (child) folder, based on setting
        /// </summary>
        [Obsolete("Import folder method not called directly from v13.1 will be removed in v15")]
        protected virtual IEnumerable<uSyncAction> ImportFolder(string folder, HandlerSettings config, Dictionary<string, TObject> updates, bool force, SyncUpdateCallback callback)
        {
            List<uSyncAction> actions = new List<uSyncAction>();
            var files = GetImportFiles(folder);

            var flags = SerializerFlags.None;
            if (force) flags |= SerializerFlags.Force;

            var cleanMarkers = new List<string>();

            int count = 0;
            int total = files.Count();
            foreach (string file in files)
            {
                count++;

                callback?.Invoke($"Importing {Path.GetFileNameWithoutExtension(file)}", count, total);

                var result = Import(file, config, flags);
                foreach (var attempt in result)
                {
                    if (attempt.Success)
                    {
                        if (attempt.Change == ChangeType.Clean)
                        {
                            cleanMarkers.Add(file);
                        }
                        else if (attempt.Item != null && attempt.Item is TObject item)
                        {
                            updates.Add(file, item);
                        }
                    }

                    if (attempt.Change != ChangeType.Clean)
                        actions.Add(attempt);
                }
            }

            // bulk save ..
            if (flags.HasFlag(SerializerFlags.DoNotSave) && updates.Any())
            {
                // callback?.Invoke($"Saving {updates.Count()} changes", 1, 1);
                serializer.Save(updates.Select(x => x.Value));
            }

            var folders = syncFileService.GetDirectories(folder);
            foreach (var children in folders)
            {
                actions.AddRange(ImportFolder(children, config, updates, force, callback));
            }

            if (actions.All(x => x.Success) && cleanMarkers.Count > 0)
            {
                foreach (var item in cleanMarkers.Select((filePath, Index) => new { filePath, Index }))
                {
                    var folderName = Path.GetFileName(item.filePath);
                    callback?.Invoke($"Cleaning {folderName}", item.Index, cleanMarkers.Count);

                    var cleanActions = CleanFolder(item.filePath, false, config.UseFlatStructure);
                    if (cleanActions.Any())
                    {
                        actions.AddRange(cleanActions);
                    }
                    else
                    {
                        // nothing to delete, we report this as a no change 
                        actions.Add(uSyncAction.SetAction(true, $"Folder {Path.GetFileName(item.filePath)}", change: ChangeType.NoChange, filename: item.filePath));
                    }
                }
                // remove the actual cleans (they will have been replaced by the deletes
                actions.RemoveAll(x => x.Change == ChangeType.Clean);
            }

            return actions;
        }

        /// <summary>
        ///  Import a single item, from the .config file supplied
        /// </summary>
        public virtual IEnumerable<uSyncAction> Import(string filePath, HandlerSettings config, SerializerFlags flags)
        {
            try
            {
                syncFileService.EnsureFileExists(filePath);
                var node = syncFileService.LoadXElement(filePath);
                return Import(node, filePath, config, flags);
            }
            catch (FileNotFoundException notFoundException)
            {
                return uSyncAction.Fail(Path.GetFileName(filePath), this.handlerType, this.ItemType, ChangeType.Fail, $"File not found {notFoundException.Message}", notFoundException)
                    .AsEnumerableOfOne();
            }
            catch (Exception ex)
            {
                logger.LogWarning("{alias}: Import Failed : {exception}", this.Alias, ex.ToString());
                return uSyncAction.Fail(Path.GetFileName(filePath), this.handlerType, this.ItemType, ChangeType.Fail, $"Import Fail: {ex.Message}", new Exception(ex.Message, ex))
                    .AsEnumerableOfOne();
            }
        }

        /// <summary>
        /// Import a single item based on already loaded XML
        /// </summary>
        public virtual IEnumerable<uSyncAction> Import(XElement node, string filename, HandlerSettings config, SerializerFlags flags)
        {
            if (config.FailOnMissingParent) flags |= SerializerFlags.FailMissingParent;
            return ImportElement(node, filename, config, new uSyncImportOptions { Flags = flags });
        }

        /// <summary>
        ///  Import a single item from a usync XML file
        /// </summary>
        virtual public IEnumerable<uSyncAction> Import(string file, HandlerSettings config, bool force)
        {
            var flags = SerializerFlags.OnePass;
            if (force) flags |= SerializerFlags.Force;

            return Import(file, config, flags);
        }

        /// <summary>
        /// Import a node, with settings and options 
        /// </summary>
        /// <remarks>
        ///  All Imports lead here
        /// </remarks>
        virtual public IEnumerable<uSyncAction> ImportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options)
        {
            if (!ShouldImport(node, settings))
            {
                return uSyncAction.SetAction(true, node.GetAlias(), message: "Change blocked (based on configuration)")
                    .AsEnumerableOfOne();
            }

            if (_mutexService.FireItemStartingEvent(new uSyncImportingItemNotification(node, (ISyncHandler)this)))
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
                var attempt = DeserializeItem(node, serializerOptions);
                var action = uSyncActionHelper<TObject>.SetAction(attempt, GetNameFromFileOrNode(filename, node), node.GetKey(), this.Alias, IsTwoPass);

                // add item if we have it.
                if (attempt.Item != null) action.Item = attempt.Item;

                // add details if we have them
                if (attempt.Details != null && attempt.Details.Any()) action.Details = attempt.Details;

                // this might not be the place to do this because, two pass items are imported at another point too.
                _mutexService.FireItemCompletedEvent(new uSyncImportedItemNotification(node, attempt.Change));


                return action.AsEnumerableOfOne();
            }
            catch (Exception ex)
            {
                logger.LogWarning("{alias}: Import Failed : {exception}", this.Alias, ex.ToString());
                return uSyncAction.Fail(Path.GetFileName(filename), this.handlerType, this.ItemType, ChangeType.Fail,
                    $"{this.Alias} Import Fail: {ex.Message}", new Exception(ex.Message))
                    .AsEnumerableOfOne();
            }

        }


        /// <summary>
        ///  Works through a list of items that have been processed and performs the second import pass on them.
        /// </summary>
        private void PerformSecondPassImports(List<ImportedItem<TObject>> importedItems, List<uSyncAction> actions, HandlerSettings config, SyncUpdateCallback callback = null)
        {
            foreach (var item in importedItems.Select((update, Index) => new { update, Index }))
            {
                var itemKey = item.update.Node.GetKey();

                callback?.Invoke($"Second Pass {item.update.Node.GetKey()}", item.Index, importedItems.Count);
                var attempt = ImportSecondPass(item.update.Node, item.update.Item, config, callback);
                if (attempt.Success)
                {
                    // if the second attempt has a message on it, add it to the first attempt.
                    if (!string.IsNullOrWhiteSpace(attempt.Message) || attempt.Details?.Any() == true)
                    {
                        uSyncAction action = actions.FirstOrDefault(x => $"{x.key}_{x.HandlerAlias}" == $"{itemKey}_{this.Alias}", new uSyncAction { key = Guid.Empty });
                        if (action.key != Guid.Empty)
                        {
                            actions.Remove(action);
                            action.Message += attempt.Message ?? "";

                            if (attempt.Details?.Any() == true)
                            {
                                var details = action.Details.ToList();
                                details.AddRange(attempt.Details);
                                action.Details = details;
                            }
                            actions.Add(action);
                        }
                    }
                    if (attempt.Change > ChangeType.NoChange && !attempt.Saved && attempt.Item != null)
                    {
                        serializer.Save(attempt.Item.AsEnumerableOfOne());
                    }
                }
                else
                {
                    uSyncAction action = actions.FirstOrDefault(x => $"{x.key}_{x.HandlerAlias}" == $"{itemKey}_{this.Alias}", new uSyncAction { key = Guid.Empty });
                    if (action.key != Guid.Empty)
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
        virtual public IEnumerable<uSyncAction> ImportSecondPass(uSyncAction action, HandlerSettings settings, uSyncImportOptions options)
        {
            if (!IsTwoPass) return Enumerable.Empty<uSyncAction>();

            try
            {
                var file = action.FileName;

                if (!syncFileService.FileExists(file))
                    return Enumerable.Empty<uSyncAction>();

                var node = syncFileService.LoadXElement(file);
                var item = GetFromService(node.GetKey());
                if (item == null) return Enumerable.Empty<uSyncAction>();

                // merge the options from the handler and any import options into our serializer options.
                var serializerOptions = new SyncSerializerOptions(options?.Flags ?? SerializerFlags.None, settings.Settings, options?.UserId ?? -1);
                serializerOptions.MergeSettings(options?.Settings);

                // do the second pass on this item
                var result = DeserializeItemSecondPass(item, node, serializerOptions);

                return uSyncActionHelper<TObject>.SetAction(result, file, node.GetKey(), this.Alias).AsEnumerableOfOne();
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Second Import Failed: {ex}");
                return uSyncAction.Fail(action.Name, this.handlerType, action.ItemType, ChangeType.ImportFail, "Second import failed", ex).AsEnumerableOfOne();
            }
        }


        /// <summary>
        ///  Perform a 'second pass' import on a single item.
        /// </summary>
        [Obsolete("Call method with node element to reduce disk IO, will be removed in v15")]
        virtual public SyncAttempt<TObject> ImportSecondPass(string file, TObject item, HandlerSettings config, SyncUpdateCallback callback)
        {
            if (IsTwoPass)
            {
                try
                {
                    syncFileService.EnsureFileExists(file);

                    var flags = SerializerFlags.None;

                    var node = syncFileService.LoadXElement(file);
                    return DeserializeItemSecondPass(item, node, new SyncSerializerOptions(flags, config.Settings));
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Second Import Failed: {ex.ToString()}");
                    return SyncAttempt<TObject>.Fail(GetItemAlias(item), item, ChangeType.Fail, ex.Message, ex);
                }
            }

            return SyncAttempt<TObject>.Succeed(GetItemAlias(item), ChangeType.NoChange);
        }

        /// <summary>
        ///  Perform a 'second pass' import on a single item.
        /// </summary>
        virtual public SyncAttempt<TObject> ImportSecondPass(XElement node, TObject item, HandlerSettings config, SyncUpdateCallback callback)
        {
            if (IsTwoPass is false)
                return SyncAttempt<TObject>.Succeed(GetItemAlias(item), ChangeType.NoChange);

            try
            {
                return DeserializeItemSecondPass(item, node, new SyncSerializerOptions(SerializerFlags.None, config.Settings));
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Second Import Failed: {ex.ToString()}");
                return SyncAttempt<TObject>.Fail(GetItemAlias(item), item, ChangeType.Fail, ex.Message, ex);
            }
        }

        /// <summary>
        ///  given a folder we calculate what items we can remove, because they are 
        ///  not in one the files in the folder.
        /// </summary>
        protected virtual IEnumerable<uSyncAction> CleanFolder(string cleanFile, bool reportOnly, bool flat)
        {
            var folder = Path.GetDirectoryName(cleanFile);
            if (!Directory.Exists(folder)) return Enumerable.Empty<uSyncAction>();


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
                var parent = GetCleanParent(cleanFile);
                if (parent == null) return Enumerable.Empty<uSyncAction>();

                logger.LogDebug("Got parent with {alias} from clean file {file}", GetItemAlias(parent), Path.GetFileName(cleanFile));

                // keys should aways have at least one entry (the key from cleanFile)
                // if it doesn't then something might have gone wrong.
                // because we are being defensive when it comes to deletes, 
                // we only then do deletes when we know we have loaded some keys!
                return DeleteMissingItems(parent, keys, reportOnly);
            }
            else
            {
                logger.LogWarning("Failed to get the keys for items in the folder, there might be a disk issue {folder}", folder);
                return Enumerable.Empty<uSyncAction>();
            }
        }

        /// <summary>
        ///  pre-populates the cache folder key list. 
        /// </summary>
        /// <remarks>
        ///  this means if we are calling the process multiple times, 
        ///  we can optimise the key code and only load it once. 
        /// </remarks>
        public void PreCacheFolderKeys(string folder, IList<Guid> folderKeys)
        {
            var cacheKey = $"{GetCacheKeyBase()}_{folder.GetHashCode()}";
            runtimeCache.ClearByKey(cacheKey);
            runtimeCache.GetCacheItem(cacheKey, () => folderKeys);
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

            }, null);
        }

        /// <summary>
        ///  Get the parent item of the clean file (so we can check if the folder has any versions of this item in it)
        /// </summary>
        protected TObject GetCleanParent(string file)
        {
            var node = XElement.Load(file);
            var key = node.GetKey();
            if (key == Guid.Empty) return default;
            return GetFromService(key);
        }

        /// <summary>
        ///  remove an items that are not listed in the GUIDs to keep
        /// </summary>
        /// <param name="parent">parent item that all keys will be under</param>
        /// <param name="keysToKeep">list of GUIDs of items we don't want to delete</param>
        /// <param name="reportOnly">will just report what would happen (doesn't do the delete)</param>
        /// <returns>list of delete actions</returns>
        protected abstract IEnumerable<uSyncAction> DeleteMissingItems(TObject parent, IEnumerable<Guid> keysToKeep, bool reportOnly);

        /// <summary>
        /// Remove an items that are not listed in the GUIDs to keep.
        /// </summary>
        /// <param name="parentId">parent item that all keys will be under</param>
        /// <param name="keysToKeep">list of GUIDs of items we don't want to delete</param>
        /// <param name="reportOnly">will just report what would happen (doesn't do the delete)</param>
        /// <returns>list of delete actions</returns>
        protected virtual IEnumerable<uSyncAction> DeleteMissingItems(int parentId, IEnumerable<Guid> keysToKeep, bool reportOnly)
            => Enumerable.Empty<uSyncAction>();

        /// <summary>
        ///  Get the files we are going to import from a folder. 
        /// </summary>
        protected virtual IEnumerable<string> GetImportFiles(string folder)
            => syncFileService.GetFiles(folder, $"*.{this.uSyncConfig.Settings.DefaultExtension}").OrderBy(x => x);

        /// <summary>
        ///  check to see if this element should be imported as part of the process.
        /// </summary>
        virtual protected bool ShouldImport(XElement node, HandlerSettings config)
        {
            // if createOnly is on, then we only create things that are not already there. 
            // this lookup is slow (relatively) so we only do it if we have to.
            if (config.GetSetting(Core.uSyncConstants.DefaultSettings.CreateOnly, Core.uSyncConstants.DefaultSettings.CreateOnly_Default)
                || config.GetSetting(Core.uSyncConstants.DefaultSettings.OneWay, Core.uSyncConstants.DefaultSettings.CreateOnly_Default))
            {
                var item = serializer.FindItem(node);
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
        virtual protected bool ShouldExport(XElement node, HandlerSettings config) => true;

        #endregion

        #region Exporting

        /// <summary>
        /// Export all items to a give folder on the disk
        /// </summary>
        virtual public IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings config, SyncUpdateCallback callback)
            => ExportAll([folder], config, callback);

        /// <summary>
        /// Export all items to a give folder on the disk
        /// </summary>
        virtual public IEnumerable<uSyncAction> ExportAll(string[] folders, HandlerSettings config, SyncUpdateCallback callback)
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
        virtual public IEnumerable<uSyncAction> ExportAll(TContainer parent, string folder, HandlerSettings config, SyncUpdateCallback callback)
            => ExportAll(parent, [folder], config, callback);

        /// <summary>
        /// Export all items to a give folder on the disk
        /// </summary>
        virtual public IEnumerable<uSyncAction> ExportAll(TContainer parent, string[] folders, HandlerSettings config, SyncUpdateCallback callback)
        {
            var actions = new List<uSyncAction>();

            if (itemContainerType != UmbracoObjectTypes.Unknown)
            {
                var containers = GetFolders(parent);
                foreach (var container in containers)
                {
                    actions.AddRange(ExportAll(container, folders, config, callback));
                }
            }

            var items = GetChildItems(parent).ToList();
            foreach (var item in items.Select((Value, Index) => new { Value, Index }))
            {
                TObject concreteType;
                if (item.Value is TObject t)
                {
                    concreteType = t;
                }
                else
                {
                    concreteType = GetFromService(item.Value);
                }
                if (concreteType != null)
                {  // only export the items (not the containers).
                    callback?.Invoke(GetItemName(concreteType), item.Index, items.Count);
                    actions.AddRange(Export(concreteType, folders, config));
                }
                actions.AddRange(ExportAll(item.Value, folders, config, callback));
            }

            return actions;
        }

        /// <summary>
        /// Fetch all child items beneath a given container 
        /// </summary>
        abstract protected IEnumerable<TContainer> GetChildItems(TContainer parent);

        /// <summary>
        /// Fetch all child items beneath a given folder
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        abstract protected IEnumerable<TContainer> GetFolders(TContainer parent);

        /// <summary>
        /// Does this container have any children 
        /// </summary>
        public bool HasChildren(TContainer item)
            => GetFolders(item).Any() || GetChildItems(item).Any();


        /// <summary>
        /// Export a single item based on it's ID
        /// </summary>
        public IEnumerable<uSyncAction> Export(int id, string folder, HandlerSettings settings)
            => Export(id, [folder], settings);

        /// <summary>
        ///  Export an item based on its id, observing root behavior. 
        /// </summary>
        public IEnumerable<uSyncAction> Export(int id, string[] folders, HandlerSettings settings)
        {
            var item = this.GetFromService(id);
            return this.Export(item, folders, settings);
        }

        /// <summary>
        /// Export an single item from a given UDI value
        /// </summary>
        public IEnumerable<uSyncAction> Export(Udi udi, string folder, HandlerSettings settings)
            => Export(udi, [folder], settings);

        /// <summary>
        /// Export an single item from a given UDI value
        /// </summary>
        public IEnumerable<uSyncAction> Export(Udi udi, string[] folders, HandlerSettings settings)
        {
            var item = FindByUdi(udi);
            if (item != null)
                return Export(item, folders, settings);

            return uSyncAction.Fail(nameof(udi), this.handlerType, this.ItemType, ChangeType.Fail, $"Item not found {udi}",
                 new KeyNotFoundException(nameof(udi)))
                .AsEnumerableOfOne();
        }

        /// <summary>
        /// Export a given item to disk
        /// </summary>
        virtual public IEnumerable<uSyncAction> Export(TObject item, string folder, HandlerSettings config)
            => Export(item, [folder], config);

        /// <summary>
        /// Export a given item to disk
        /// </summary>
        virtual public IEnumerable<uSyncAction> Export(TObject item, string[] folders, HandlerSettings config)
        {
            if (item == null)
                return uSyncAction.Fail(nameof(item), this.handlerType, this.ItemType, ChangeType.Fail, "Item not set",
                    new ArgumentNullException(nameof(item))).AsEnumerableOfOne();

            if (_mutexService.FireItemStartingEvent(new uSyncExportingItemNotification<TObject>(item, (ISyncHandler)this)))
            {
                return uSyncActionHelper<TObject>
                    .ReportAction(ChangeType.NoChange, GetItemName(item), string.Empty, string.Empty, GetItemKey(item), this.Alias,
                                    "Change stopped by delegate event")
                    .AsEnumerableOfOne();
            }

            var targetFolder = folders.Last();

            var filename = GetPath(targetFolder, item, config.GuidNames, config.UseFlatStructure)
                .ToAppSafeFileName();

            // 
            if (IsLockedAtRoot(folders, filename.Substring(targetFolder.Length+1)))
            {
                // if we have lock roots on, then this item will not export 
                // because exporting would mean the root was no longer used.
                return uSyncAction.SetAction(true, filename,
                    type: typeof(TObject).ToString(),
                    change: ChangeType.NoChange,
                    message: "Not exported (would overwrite root value)",
                    filename: filename).AsEnumerableOfOne();
            }


            var attempt = Export_DoExport(item, filename, folders, config);

            if (attempt.Change > ChangeType.NoChange)
                _mutexService.FireItemCompletedEvent(new uSyncExportedItemNotification(attempt.Item, ChangeType.Export));

            return uSyncActionHelper<XElement>.SetAction(attempt, filename, GetItemKey(item), this.Alias).AsEnumerableOfOne();
        }

        /// <summary>
        ///  Do the meat of the export 
        /// </summary>
        /// <remarks>
        ///  inheriting this method, means you don't have to repeat all the checks in child handlers. 
        /// </remarks>
        protected virtual SyncAttempt<XElement> Export_DoExport(TObject item, string filename, string[] folders, HandlerSettings config)
        {
            var attempt = SerializeItem(item, new SyncSerializerOptions(config.Settings));
            if (attempt.Success)
            {
                if (ShouldExport(attempt.Item, config))
                {
                    // only write the file to disk if it should be exported.
                    syncFileService.SaveXElement(attempt.Item, filename);

                    if (config.CreateClean && HasChildren(item))
                    {
                        CreateCleanFile(GetItemKey(item), filename);
                    }
                }
                else
                {
                    return SyncAttempt<XElement>.Succeed(filename, ChangeType.NoChange, "Not Exported (Based on configuration)");
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
        protected virtual bool HasChildren(TObject item)
            => true;

        /// <summary>
        ///  create a clean file, which is used as a marker, when performing remote deletes.
        /// </summary>
        protected void CreateCleanFile(Guid key, string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || key == Guid.Empty)
                return;

            var folder = Path.GetDirectoryName(filename);
            var name = Path.GetFileNameWithoutExtension(filename);

            var cleanPath = Path.Combine(folder, $"{name}_clean.config");

            var node = XElementExtensions.MakeEmpty(key, SyncActionType.Clean, $"clean {name} children");
            node.Add(new XAttribute("itemType", serializer.ItemType));
            syncFileService.SaveXElement(node, cleanPath);
        }

        #endregion

        #region Reporting 

        /// <summary>
        /// Run a report based on a given folder
        /// </summary>
        public IEnumerable<uSyncAction> Report(string folder, HandlerSettings config, SyncUpdateCallback callback)
            => Report([folder], config, callback);

        /// <summary>
        /// Run a report based on a set of folders. 
        /// </summary>
        public IEnumerable<uSyncAction> Report(string[] folders, HandlerSettings config, SyncUpdateCallback callback)
        {
            List<uSyncAction> actions = [];

            var cacheKey = PrepCaches();

            callback?.Invoke("Checking Actions", 1, 3);

            var items = GetMergedItems(folders);

            int count = 0;

            foreach (var item in items)
            {
                count++;
                callback?.Invoke(Path.GetFileNameWithoutExtension(item.Path), count, items.Count);
                actions.AddRange(ReportElement(item.Node, item.Filename, config));
            }

            callback?.Invoke("Validating Report", 2, 3);
            var validationActions = ReportMissingParents(actions.ToArray());
            actions.AddRange(ReportDeleteCheck(uSyncConfig.GetRootFolder(), validationActions));

            CleanCaches(cacheKey);
            callback?.Invoke("Done", 3, 3);
            return actions;
        }

        private List<uSyncAction> ValidateReport(string folder, List<uSyncAction> actions)
        {
            // Alters the existing list, by changing the type as needed.
            var validationActions = ReportMissingParents(actions.ToArray());

            // adds new actions - for delete clashes.
            validationActions.AddRange(ReportDeleteCheck(folder, validationActions));

            return validationActions.ToList();
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
                    var filename = Path.GetFileName(deleteAction.FileName);
                    var relativePath = deleteAction.FileName.Substring(folder.Length);

                    details.Add(uSyncChange.Delete(filename, $"Delete: {deleteAction.Name} ({filename}", relativePath));

                    // add all the duplicates to the list of changes.
                    foreach (var dup in actions.Where(x => x.Change != ChangeType.Delete && DoActionsMatch(x, deleteAction)))
                    {
                        var dupFilename = Path.GetFileName(dup.FileName);
                        var dupRelativePath = dup.FileName.Substring(folder.Length);

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
            if (a.key == b.key) return true;
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
                if (actions[i].Change != ChangeType.ParentMissing) continue;

                var node = XElement.Load(actions[i].FileName);
                var guid = node.GetParentKey();

                if (guid != Guid.Empty)
                {
                    if (actions.Any(x => x.key == guid && (x.Change < ChangeType.Fail || x.Change == ChangeType.ParentMissing)))
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

            return actions.ToList();
        }

        /// <summary>
        ///  Run a report on a given folder
        /// </summary>
        public virtual IEnumerable<uSyncAction> ReportFolder(string folder, HandlerSettings config, SyncUpdateCallback callback)
        {

            List<uSyncAction> actions = new List<uSyncAction>();

            var files = GetImportFiles(folder).ToList();

            int count = 0;

            logger.LogDebug("ReportFolder: {folder} ({count} files)", folder, files.Count);

            foreach (string file in files)
            {
                count++;
                callback?.Invoke(Path.GetFileNameWithoutExtension(file), count, files.Count);

                actions.AddRange(ReportItem(file, config));
            }

            foreach (var children in syncFileService.GetDirectories(folder))
            {
                actions.AddRange(ReportFolder(children, config, callback));
            }

            return actions;
        }

        /// <summary>
        /// Report on any changes for a single XML node.
        /// </summary>
        protected virtual IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings config)
            => ReportElement(node, filename, config ?? this.DefaultConfig, new uSyncImportOptions());


        /// <summary>
        ///  Report an Element
        /// </summary>
        public IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options)
        {
            try
            {
                //  starting reporting notification
                //  this lets us intercept a report and 
                //  shortcut the checking (sometimes).
                if (_mutexService.FireItemStartingEvent(new uSyncReportingItemNotification(node)))
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
                var change = IsItemCurrent(node, serializerOptions);

                var action = uSyncActionHelper<TObject>
                        .ReportAction(change.Change, node.GetAlias(), node.GetPath(), GetNameFromFileOrNode(filename, node), node.GetKey(), this.Alias, "");



                action.Message = "";

                if (action.Change == ChangeType.Clean)
                {
                    actions.AddRange(CleanFolder(filename, true, settings.UseFlatStructure));
                }
                else if (action.Change > ChangeType.NoChange)
                {
                    action.Details = GetChanges(node, change.CurrentNode, serializerOptions);
                    if (action.Change != ChangeType.Create && (action.Details == null || action.Details.Count() == 0))
                    {
                        action.Message = "XML is different - but properties may not have changed";
                        action.Details = MakeRawChange(node, change.CurrentNode, serializerOptions).AsEnumerableOfOne();
                    }
                    else
                    {
                        action.Message = $"{action.Change}";
                    }
                    actions.Add(action);
                }
                else
                {
                    actions.Add(action);
                }

                // tell other things we have reported this item.
                _mutexService.FireItemCompletedEvent(new uSyncReportedItemNotification(node, action.Change));

                return actions;
            }
            catch (FormatException fex)
            {
                return uSyncActionHelper<TObject>
                    .ReportActionFail(Path.GetFileName(node.GetAlias()), $"format error {fex.Message}")
                    .AsEnumerableOfOne();
            }
        }

        private uSyncChange MakeRawChange(XElement node, XElement current, SyncSerializerOptions options)
        {
            if (current != null)
                return uSyncChange.Update(node.GetAlias(), "Raw XML", current.ToString(), node.ToString());

            return uSyncChange.NoChange(node.GetAlias(), node.GetAlias());
        }

        /// <summary>
        /// Run a report on a single file.
        /// </summary>
        protected IEnumerable<uSyncAction> ReportItem(string file, HandlerSettings config)
        {
            try
            {
                var node = syncFileService.LoadXElement(file);

                if (ShouldImport(node, config))
                {
                    return ReportElement(node, file, config);
                }
                else
                {
                    return uSyncActionHelper<TObject>.ReportAction(ChangeType.NoChange, node.GetAlias(), node.GetPath(), file, node.GetKey(),
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


        private IEnumerable<uSyncChange> GetChanges(XElement node, XElement currentNode, SyncSerializerOptions options)
            => itemFactory.GetChanges<TObject>(node, currentNode, options);

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
        public virtual void Handle(DeletedNotification<TObject> notification)
        {
            if (!ShouldProcessEvent()) return;

            foreach (var item in notification.DeletedEntities)
            {
                try
                {
                    var handlerFolders = GetDefaultHandlerFolders();
                    ExportDeletedItem(item, handlerFolders, DefaultConfig);
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
        public virtual void Handle(SavedNotification<TObject> notification)
        {
            if (!ShouldProcessEvent()) return;

            var handlerFolders = GetDefaultHandlerFolders();

            foreach (var item in notification.SavedEntities)
            {
                try
                {
                    var attempts = Export(item, handlerFolders, DefaultConfig);
                    foreach (var attempt in attempts.Where(x => x.Success))
                    {
                        this.CleanUp(item, attempt.FileName, handlerFolders.Last());
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
        public virtual void Handle(MovedNotification<TObject> notification)
        {
            try
            {
                if (!ShouldProcessEvent()) return;
                HandleMove(notification.MoveInfoCollection);
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
        protected void HandleMove(IEnumerable<MoveEventInfo<TObject>> moveInfoCollection)
        {
            foreach (var item in moveInfoCollection)
            {
                var handlerFolders = GetDefaultHandlerFolders();
                var attempts = Export(item.Entity, handlerFolders, DefaultConfig);

                if (!this.DefaultConfig.UseFlatStructure)
                {
                    // moves only need cleaning up if we are not using flat, because 
                    // with flat the file will always be in the same folder.

                    foreach (var attempt in attempts.Where(x => x.Success))
                    {
                        this.CleanUp(item.Entity, attempt.FileName, handlerFolders.Last());
                    }
                }
            }
        }

        /// <summary>
        /// Export any deletes items to disk 
        /// </summary>
        /// <remarks>
        /// Deleted items get 'empty' files on disk so we know they where deleted
        /// </remarks>
        protected virtual void ExportDeletedItem(TObject item, string[] folders, HandlerSettings config)
        {
            if (item == null) return;

            var targetFolder = folders.Last();

            var filename = GetPath(targetFolder, item, config.GuidNames, config.UseFlatStructure)
                .ToAppSafeFileName();

            if (IsLockedAtRoot(folders, filename.Substring(targetFolder.Length + 1)))
            {
                // don't do anything this thing exists at a higher level. ! 
                return;
            }
            

            var attempt = serializer.SerializeEmpty(item, SyncActionType.Delete, string.Empty);
            if (ShouldExport(attempt.Item, config))
            {
                if (attempt.Success && attempt.Change != ChangeType.NoChange)
                {
                    syncFileService.SaveXElement(attempt.Item, filename);

                    // so check - it shouldn't (under normal operation) 
                    // be possible for a clash to exist at delete, because nothing else 
                    // will have changed (like name or location) 

                    // we only then do this if we are not using flat structure. 
                    if (!DefaultConfig.UseFlatStructure)
                        this.CleanUp(item, filename, Path.Combine(folders.Last(), this.DefaultFolder));
                }
            }
        }

        /// <summary>
        ///  get all the possible folders for this handlers 
        /// </summary>
        protected string[] GetDefaultHandlerFolders()
            => rootFolders.Select(f => Path.Combine(f, DefaultFolder)).ToArray();


        /// <summary>
        ///  Cleans up the handler folder, removing duplicate files for this item
        ///  </summary>
        ///  <remarks>
        ///   e.g if someone renames a thing (and we are using the name in the file) 
        ///   this will clean anything else in the folder that has that key / alias
        ///  </remarks>
        protected virtual void CleanUp(TObject item, string newFile, string folder)
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
                        var node = syncFileService.LoadXElement(file);

                        // if this XML file matches the item we have just saved. 

                        if (!node.IsEmptyItem() || node.GetEmptyAction() != SyncActionType.Rename)
                        {
                            // the node isn't empty, or its not a rename (because all clashes become renames)

                            if (DoItemsMatch(node, item))
                            {
                                logger.LogDebug("Duplicate {file} of {alias}, saving as rename", Path.GetFileName(file), this.GetItemAlias(item));

                                var attempt = serializer.SerializeEmpty(item, SyncActionType.Rename, node.GetAlias());
                                if (attempt.Success)
                                {
                                    syncFileService.SaveXElement(attempt.Item, file);
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
                CleanUp(item, newFile, children);
            }
        }

        #endregion

        // 98% of the time the serializer can do all these calls for us, 
        // but for blueprints, we want to get different items, (but still use the 
        // content serializer) so we override them.


        /// <summary>
        /// Fetch an item via the Serializer
        /// </summary>
        protected virtual TObject GetFromService(int id) => serializer.FindItem(id);

        /// <summary>
        /// Fetch an item via the Serializer
        /// </summary>
        protected virtual TObject GetFromService(Guid key) => serializer.FindItem(key);

        /// <summary>
        /// Fetch an item via the Serializer
        /// </summary>
        protected virtual TObject GetFromService(string alias) => serializer.FindItem(alias);

        /// <summary>
        /// Delete an item via the Serializer
        /// </summary>
        protected virtual void DeleteViaService(TObject item) => serializer.DeleteItem(item);

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
        abstract protected TObject GetFromService(TContainer item);

        /// <summary>
        /// Get a container item from the Umbraco service.
        /// </summary>
        virtual protected TContainer GetContainer(Guid key) => default;

        /// <summary>
        /// Get a container item from the Umbraco service.
        /// </summary>
        virtual protected TContainer GetContainer(int id) => default;

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
        virtual protected string GetPath(string folder, TObject item, bool GuidNames, bool isFlat)
        {
            if (isFlat && GuidNames) return Path.Combine(folder, $"{GetItemKey(item)}.{this.uSyncConfig.Settings.DefaultExtension}");
            var path = Path.Combine(folder, $"{this.GetItemPath(item, GuidNames, isFlat)}.{this.uSyncConfig.Settings.DefaultExtension}");

            // if this is flat but not using GUID filenames, then we check for clashes.
            if (isFlat && !GuidNames) return CheckAndFixFileClash(path, item);
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
        virtual protected string CheckAndFixFileClash(string path, TObject item)
        {
            if (syncFileService.FileExists(path))
            {
                var node = syncFileService.LoadXElement(path);

                if (node == null) return path;
                if (GetItemKey(item) == node.GetKey()) return path;
                if (GetXmlMatchString(node) == GetItemMatchString(item)) return path;

                // get here we have a clash, we should append something
                var append = GetItemKey(item).ToShortKeyString(8); // (this is the shortened GUID like media folders do)
                return Path.Combine(Path.GetDirectoryName(path),
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
        virtual public uSyncAction Rename(TObject item) => new uSyncAction();


        /// <summary>
        ///  Group a handler belongs too (default will be settings)
        /// </summary>
        public virtual string Group { get; protected set; } = uSyncConstants.Groups.Settings;

        /// <summary>
        /// Serialize an item to XML based on a given UDI value
        /// </summary>
        public SyncAttempt<XElement> GetElement(Udi udi)
        {
            var element = FindByUdi(udi);
            if (element != null)
                return SerializeItem(element, new SyncSerializerOptions());

            return SyncAttempt<XElement>.Fail(udi.ToString(), ChangeType.Fail, "Item not found");
        }


        private TObject FindByUdi(Udi udi)
        {
            switch (udi)
            {
                case GuidUdi guidUdi:
                    return GetFromService(guidUdi.Guid);
                case StringUdi stringUdi:
                    return GetFromService(stringUdi.Id);
            }

            return default;
        }

        /// <summary>
        /// Calculate any dependencies for any given item based on loaded dependency checkers 
        /// </summary>
        /// <remarks>
        /// uSync contains no dependency checkers by default - uSync.Complete will load checkers
        /// when installed. 
        /// </remarks>
        public IEnumerable<uSyncDependency> GetDependencies(Guid key, DependencyFlags flags)
        {
            if (key == Guid.Empty)
            {
                return GetContainerDependencies(default, flags);
            }
            else
            {
                var item = this.GetFromService(key);
                if (item == null)
                {
                    var container = this.GetContainer(key);
                    if (container != null)
                    {
                        return GetContainerDependencies(container, flags);
                    }
                    return Enumerable.Empty<uSyncDependency>();
                }

                return GetDependencies(item, flags);
            }
        }

        /// <summary>
        /// Calculate any dependencies for any given item based on loaded dependency checkers 
        /// </summary>
        /// <remarks>
        /// uSync contains no dependency checkers by default - uSync.Complete will load checkers
        /// when installed. 
        /// </remarks>
        public IEnumerable<uSyncDependency> GetDependencies(int id, DependencyFlags flags)
        {
            // get them from the root. 
            if (id == -1) return GetContainerDependencies(default, flags);

            var item = this.GetFromService(id);
            if (item == null)
            {
                var container = this.GetContainer(id);
                if (container != null)
                {
                    return GetContainerDependencies(container, flags);
                }

                return Enumerable.Empty<uSyncDependency>();
            }
            return GetDependencies(item, flags);
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
        protected IEnumerable<uSyncDependency> GetDependencies(TObject item, DependencyFlags flags)
        {
            if (item == null || !HasDependencyCheckers()) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();
            foreach (var checker in dependencyCheckers)
            {
                dependencies.AddRange(checker.GetDependencies(item, flags));
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
        private IEnumerable<uSyncDependency> GetContainerDependencies(TContainer parent, DependencyFlags flags)
        {
            if (!HasDependencyCheckers()) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            var containers = GetFolders(parent);
            if (containers != null && containers.Any())
            {
                foreach (var container in containers)
                {
                    dependencies.AddRange(GetContainerDependencies(container, flags));
                }
            }

            var children = GetChildItems(parent);
            if (children != null && children.Any())
            {
                foreach (var child in children)
                {
                    var childItem = GetFromService(child);
                    if (childItem != null)
                    {
                        foreach (var checker in dependencyCheckers)
                        {
                            dependencies.AddRange(checker.GetDependencies(childItem, flags));
                        }
                    }
                }
            }


            return dependencies.SafeDistinctBy(x => x.Udi.ToString()).OrderByDescending(x => x.Order);
        }

        #region Serializer Calls 

        /// <summary>
        ///  call the serializer to get an items xml.
        /// </summary>
        protected SyncAttempt<XElement> SerializeItem(TObject item, SyncSerializerOptions options)
            => serializer.Serialize(item, options);

        /// <inheritdoc />
        /// <summary>
        ///  turn the xml into an item (and optionally save it to umbraco).
        /// </summary>
        protected SyncAttempt<TObject> DeserializeItem(XElement node, SyncSerializerOptions options)
            => serializer.Deserialize(node, options);

        /// <summary>
        ///  perform a second pass on an item you are importing.
        /// </summary>
        protected SyncAttempt<TObject> DeserializeItemSecondPass(TObject item, XElement node, SyncSerializerOptions options)
            => serializer.DeserializeSecondPass(item, node, options);

        private SyncChangeInfo IsItemCurrent(XElement node, SyncSerializerOptions options)
        {
            var change = new SyncChangeInfo();
            change.CurrentNode = SerializeFromNode(node, options);
            change.Change = serializer.IsCurrent(node, change.CurrentNode, options);
            return change;
        }
        private XElement SerializeFromNode(XElement node, SyncSerializerOptions options)
        {
            var item = serializer.FindItem(node);
            if (item != null)
            {
                var cultures = node.GetCultures();
                if (!string.IsNullOrWhiteSpace(cultures))
                {
                    // the cultures we serialize should match any in the file.
                    // this means we then only check the same values at each end.
                    options.Settings[Core.uSyncConstants.CultureKey] = cultures;
                }

                var attempt = this.SerializeItem(item, options);
                if (attempt.Success) return attempt.Item;
            }

            return null;
        }

        private class SyncChangeInfo
        {
            public ChangeType Change { get; set; }
            public XElement CurrentNode { get; set; }
        }

        /// <summary>
        /// Find an items UDI value based on the values in the uSync XML node
        /// </summary>
        public Udi FindFromNode(XElement node)
        {
            var item = serializer.FindItem(node);
            if (item != null)
                return Udi.Create(this.EntityType, serializer.ItemKey(item));

            return null;
        }

        /// <summary>
        /// Calculate the current status of an item compared to the XML in a potential import
        /// </summary>
        public ChangeType GetItemStatus(XElement node)
        {
            var serializerOptions = new SyncSerializerOptions(SerializerFlags.None, this.DefaultConfig.Settings);
            return this.IsItemCurrent(node, serializerOptions).Change;
        }

        #endregion

        private string GetNameFromFileOrNode(string filename, XElement node)
            => !string.IsNullOrWhiteSpace(filename) ? filename : node.GetAlias();


        /// <summary>
        ///  get thekey for any caches we might call (thread based cache value)
        /// </summary>
        /// <returns></returns>
        protected string GetCacheKeyBase()
            => $"keycache_{this.Alias}_{Thread.CurrentThread.ManagedThreadId}";

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
        ///  handle when things are saving. 
        /// </summary>
        /// <remarks>
        ///  used to block saves when using roots. 
        /// </remarks>
        public virtual void Handle(SavingNotification<TObject> notification)
        {
            if (ShouldBlockRootChanges(notification.SavedEntities))
            {
                notification.Cancel = true;
                notification.Messages.Add(GetCancelMessageForRoots());
            }
        }

        public virtual void Handle(MovingNotification<TObject> notification)
        {
            if (ShouldBlockRootChanges(notification.MoveInfoCollection.Select(x => x.Entity)))
            {
                notification.Cancel = true;
                notification.Messages.Add(GetCancelMessageForRoots());
            }
        }

        public virtual void Handle(DeletingNotification<TObject> notification)
        {
            if (ShouldBlockRootChanges(notification.DeletedEntities))
            {
                notification.Cancel = true;
                notification.Messages.Add(GetCancelMessageForRoots());
            }
        }

        protected bool ShouldBlockRootChanges(IEnumerable<TObject> items)
        {
            if (!ShouldProcessEvent()) return false;

            if (uSyncConfig.Settings.LockRoot == false) return false;

            if (!HasRootFolders()) return false;

            foreach (var item in items)
            {
                if (RootItemExists(item))
                    return true;
            }

            return false;
        }

        protected EventMessage GetCancelMessageForRoots()
            => new EventMessage("Blocked", "You cannot make this change, root level items are locked", EventMessageType.Error);


        private bool HasRootFolders()
            => syncFileService.AnyFolderExists(uSyncConfig.GetFolders()[..^1]);

        private bool RootItemExists(TObject item)
        {
            foreach (var folder in uSyncConfig.GetFolders()[..^1])
            {
                var filename = GetPath(
                    Path.Combine(folder, DefaultFolder),
                    item,
                    DefaultConfig.GuidNames,
                    DefaultConfig.UseFlatStructure)
                    .ToAppSafeFileName();

                if (syncFileService.FileExists(filename))
                    return true;

            }

            return false;
        }

        #endregion
    }
}