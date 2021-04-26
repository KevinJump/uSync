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
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.BackOffice.SyncHandlers
{
    public abstract class SyncHandlerRoot<TObject, TContainer>
    {
        protected readonly ILogger<SyncHandlerRoot<TObject, TContainer>> logger;

        protected readonly SyncFileService syncFileService;
        protected readonly uSyncEventService _mutexService;

        protected readonly IList<ISyncDependencyChecker<TObject>> dependencyCheckers;
        protected readonly IList<ISyncTracker<TObject>> trackers;

        protected ISyncSerializer<TObject> serializer;

        protected readonly IAppPolicyCache runtimeCache;

        /// <summary>
        ///  Alias of the handler, used when getting settings from the config file
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
        ///  does this handler require two passes at the import (e.g datatypes import once, and then again after doctypes)
        /// </summary>
        protected bool IsTwoPass = false;

        /// <summary>
        ///  the object type of the item being processed.
        /// </summary>
        public string ItemType { get; protected set; } = typeof(TObject).ToString();

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
        protected string rootFolder { get; set; }

        /// <summary>
        ///  the UDIEntityType for the handler objects
        /// </summary>
        public string EntityType { get; protected set; }

        /// <summary>
        ///  Name of the type (object)
        /// </summary>
        public string TypeName { get; protected set; }  // we calculate these now based on the entityType ? 
        protected UmbracoObjectTypes itemObjectType { get; set; } = UmbracoObjectTypes.Unknown;

        protected UmbracoObjectTypes itemContainerType = UmbracoObjectTypes.Unknown;

        protected string handlerType;

        protected readonly ISyncItemFactory itemFactory;

        protected readonly uSyncConfigService uSyncConfig;

        protected readonly IShortStringHelper shortStringHelper;

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
            rootFolder = uSyncConfig.Settings.RootFolder;
        }

        #region Importing 

        /// <summary>
        ///  Import everything from a given folder, using the supplied config settings.
        /// </summary>
        public IEnumerable<uSyncAction> ImportAll(string folder, HandlerSettings config, bool force, SyncUpdateCallback callback = null)
        {
            // using (logger.DebugDuration(handlerType, $"Importing {Alias} {Path.GetFileName(folder)}", $"Import complete {Alias}"))
            {
                var actions = new List<uSyncAction>();
                var updates = new Dictionary<string, TObject>();

                var cacheKey = PrepCaches();

                logger.LogDebug("Clearing KeyCache {key}", cacheKey);
                runtimeCache.ClearByKey(cacheKey);

                actions.AddRange(ImportFolder(folder, config, updates, force, callback));

                if (updates.Count > 0)
                {
                    PerformSecondPassImports(updates, actions, config, callback);
                }

                logger.LogDebug("Clearing KeyCache {key}", cacheKey);

                CleanCaches(cacheKey);

                callback?.Invoke("Done", 3, 3);

                return actions;
            }
        }

        /// <summary>
        ///  Import everything in a given (child) folder, based on setting
        /// </summary>
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
                return uSyncAction.Fail(Path.GetFileName(filePath), this.handlerType, ChangeType.Fail, $"File not found {notFoundException.Message}")
                    .AsEnumerableOfOne();
            }
            catch (Exception ex)
            {
                logger.LogWarning("{alias}: Import Failed : {exception}", this.Alias, ex.ToString());
                return uSyncAction.Fail(Path.GetFileName(filePath), this.handlerType, ChangeType.Fail, $"Import Fail: {ex.Message}")
                    .AsEnumerableOfOne();
            }
        }

        public virtual IEnumerable<uSyncAction> Import(XElement node, string filename, HandlerSettings config, SerializerFlags flags)
        {
            if (config.FailOnMissingParent) flags |= SerializerFlags.FailMissingParent;
            return ImportElement(node, filename, config, new uSyncImportOptions { Flags = flags });
        }

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
                return uSyncAction.SetAction(true, node.GetAlias(), message: "Change blocked (based on config)")
                    .AsEnumerableOfOne();
            }

            if (_mutexService.FireItemStartingEvent(new uSyncImportingItemNotification(node)))
            {
                // blocked
                return uSyncActionHelper<TObject>
                    .ReportAction(ChangeType.NoChange, node.GetAlias(), GetNameFromFileOrNode(filename, node), node.GetKey(), this.Alias)
                    .AsEnumerableOfOne();
            }

            try
            {
                // merge the options from the handler and any import options into our serializer options.
                var serializerOptions = new SyncSerializerOptions(options.Flags, settings.Settings);
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
                return uSyncAction.Fail(Path.GetFileName(filename), this.handlerType, ChangeType.Fail, $"Import Fail: {ex.Message}")
                    .AsEnumerableOfOne();
            }

        }


        /// <summary>
        ///  Works through a list of items that have been processed and performs the second import pass on them.
        /// </summary>
        private void PerformSecondPassImports(IDictionary<string, TObject> updates, List<uSyncAction> actions, HandlerSettings config, SyncUpdateCallback callback = null)
        {
            foreach (var item in updates.Select((update, Index) => new { update, Index }))
            {
                callback?.Invoke($"Second Pass {Path.GetFileName(item.update.Key)}", item.Index, updates.Count);
                var attempt = ImportSecondPass(item.update.Key, item.update.Value, config, callback);
                if (attempt.Success)
                {
                    // if the second attempt has a message on it, add it to the first attempt.
                    if (!string.IsNullOrWhiteSpace(attempt.Message))
                    {
                        if (actions.Any(x => x.FileName == item.update.Key))
                        {
                            var action = actions.FirstOrDefault(x => x.FileName == item.update.Key);
                            actions.Remove(action);
                            action.Message += attempt.Message;
                            actions.Add(action);
                        }
                    }

                    // If the second attemt has change details add them to the first attempt
                    if (attempt.Details != null && attempt.Details.Any())
                    {
                        if (actions.Any(x => x.FileName == item.update.Key))
                        {
                            var action = actions.FirstOrDefault(x => x.FileName == item.update.Key);

                            var details = new List<uSyncChange>();
                            if (action.Details != null)
                            {
                                details.AddRange(action.Details);
                            }
                            details.AddRange(attempt.Details);
                            actions.Remove(action);
                            action.Details = details;
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
                    // the second attempt failed - update the action.
                    if (actions.Any(x => x.FileName == item.update.Key))
                    {
                        var action = actions.FirstOrDefault(x => x.FileName == item.update.Key);
                        actions.Remove(action);
                        action.Success = attempt.Success;
                        action.Message = $"Second Pass Fail: {attempt.Message}";
                        action.Exception = attempt.Exception;
                        actions.Add(action);
                    }
                }
            }
        }


        virtual public IEnumerable<uSyncAction> ImportSecondPass(uSyncAction action, HandlerSettings settings, uSyncImportOptions options)
        {
            if (!IsTwoPass) return Enumerable.Empty<uSyncAction>();

            try
            {
                var file = action.FileName;
                var node = syncFileService.LoadXElement(file);
                var item = GetFromService(node.GetKey());
                if (item == null) return Enumerable.Empty<uSyncAction>();

                // merge the options from the handler and any import options into our serializer options.
                var serializerOptions = new SyncSerializerOptions(options.Flags, settings.Settings);
                serializerOptions.MergeSettings(options.Settings);

                // do the second pass on this item
                var result = DeserializeItemSecondPass(item, node, serializerOptions);

                return uSyncActionHelper<TObject>.SetAction(result, file, node.GetKey(), this.Alias).AsEnumerableOfOne();
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Second Import Failed: {ex}");
                return uSyncAction.Fail(action.Name, action.ItemType, ex).AsEnumerableOfOne();
            }
        }


        /// <summary>
        ///  Perform a 'second pass' import on a single item.
        /// </summary>
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
                    return SyncAttempt<TObject>.Fail(GetItemAlias(item), ChangeType.Fail, ex.Message, ex);
                }
            }

            return SyncAttempt<TObject>.Succeed(GetItemAlias(item), ChangeType.NoChange);
        }

        /// <summary>
        ///  given a folder we calculate what items we can remove, becuase they are 
        ///  not in one the the files in the folder.
        /// </summary>
        /// <param name="cleanFile"></param>
        /// <returns></returns>
        protected virtual IEnumerable<uSyncAction> CleanFolder(string cleanFile, bool reportOnly, bool flat)
        {
            var folder = Path.GetDirectoryName(cleanFile);
            if (!Directory.Exists(folder)) return Enumerable.Empty<uSyncAction>();


            // get the keys for every item in this folder. 

            // this would works on the flat folder stucture too, 
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
        ///  Get the GUIDs for all items in a folder
        /// </summary>
        /// <remarks>
        ///  This is disk intensive, (checking the .config files all the time)
        ///  so we cache it, and if we are using the flat folder stucture, then
        ///  we only do it once, so its quicker. 
        /// </remarks>
        private IList<Guid> GetFolderKeys(string folder, bool flat)
        {
            // We only need to load all the keys once per handler (if all items are in a folder that key will be used).
            var folderKey = folder.GetHashCode();

            var cacheKey = $"{GetCacheKeyBase()}_{folderKey}";

            logger.LogDebug("Getting Folder Keys : {cacheKey}", cacheKey);

            return runtimeCache.GetCacheItem(cacheKey, () =>
            {
                // when it's not flat structure we also get the sub folders. (extra defensive get them all)
                var keys = new List<Guid>();
                var files = syncFileService.GetFiles(folder, "*.config", !flat).ToList();

                foreach (var file in files)
                {
                    var node = XElement.Load(file);
                    var key = node.GetKey();
                    if (!keys.Contains(key))
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
        ///  remove an items that are not listed in the guids to keep
        /// </summary>
        /// <param name="parent">parent item that all keys will be under</param>
        /// <param name="keysToKeep">list of guids of items we don't want to delete</param>
        /// <param name="reportOnly">will just report what would happen (doesn't do the delete)</param>
        /// <returns>list of delete actions</returns>
        protected abstract IEnumerable<uSyncAction> DeleteMissingItems(TObject parent, IEnumerable<Guid> keysToKeep, bool reportOnly);

        /// <summary>
        ///  Get the files we are going to import from a folder. 
        /// </summary>
        protected virtual IEnumerable<string> GetImportFiles(string folder)
            => syncFileService.GetFiles(folder, "*.config").OrderBy(x => x);

        /// <summary>
        ///  check to see if this element should be imported as part of the process.
        /// </summary>
        virtual protected bool ShouldImport(XElement node, HandlerSettings config)
        {
            // if createOnly is on, then we only create things that are not already there. 
            // this lookup is slow(ish) so we only do it if we have to.
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
        ///  Check to see if this elment should be exported. 
        /// </summary>
        virtual protected bool ShouldExport(XElement node, HandlerSettings config) => true;

        #endregion

        #region Exporting
        virtual public IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            // we dont clean the folder out on an export all. 
            // because the actions (renames/deletes) live in the folder
            //
            // there will have to be a different clean option
            ///
            // syncFileService.CleanFolder(folder);

            return ExportAll(default, folder, config, callback);
        }

        virtual public IEnumerable<uSyncAction> ExportAll(TContainer parent, string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            var actions = new List<uSyncAction>();

            if (itemContainerType != UmbracoObjectTypes.Unknown)
            {
                var containers = GetFolders(parent);
                foreach (var container in containers)
                {
                    actions.AddRange(ExportAll(container, folder, config, callback));
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
                    actions.AddRange(Export(concreteType, folder, config));
                }
                actions.AddRange(ExportAll(item.Value, folder, config, callback));
            }

            return actions;
        }

        abstract protected IEnumerable<TContainer> GetChildItems(TContainer parent);
        abstract protected IEnumerable<TContainer> GetFolders(TContainer parent);

        public bool HasChildren(TContainer item)
            => GetFolders(item).Any() || GetChildItems(item).Any();


        public IEnumerable<uSyncAction> Export(int id, string folder, HandlerSettings settings)
        {
            var item = this.GetFromService(id);
            return this.Export(item, folder, settings);
        }

        public IEnumerable<uSyncAction> Export(Udi udi, string folder, HandlerSettings settings)
        {
            var item = FindByUdi(udi);
            if (item != null)
                return Export(item, folder, settings);

            return uSyncAction.Fail(nameof(udi), typeof(TObject).ToString(), ChangeType.Fail, "Item not found")
                .AsEnumerableOfOne();
        }


        virtual public IEnumerable<uSyncAction> Export(TObject item, string folder, HandlerSettings config)
        {
            if (item == null)
                return uSyncAction.Fail(nameof(item), typeof(TObject).ToString(), ChangeType.Fail, "Item not set").AsEnumerableOfOne();

            if (_mutexService.FireItemStartingEvent(new uSyncExportingItemNotification<TObject>(item))) 
            {
                return uSyncActionHelper<TObject>
                    .ReportAction(ChangeType.NoChange, GetItemName(item), string.Empty, GetItemKey(item), this.Alias)
                    .AsEnumerableOfOne();
            }

            var filename = GetPath(folder, item, config.GuidNames, config.UseFlatStructure);

            var attempt = SerializeItem(item, new SyncSerializerOptions(config.Settings));
            if (attempt.Success)
            {
                if (ShouldExport(attempt.Item, config))
                {
                    // only write the file to disk if it should be exported.
                    syncFileService.SaveXElement(attempt.Item, filename);
                }
                else
                {
                    return uSyncAction.SetAction(true, filename, type: typeof(TObject).ToString(), change: ChangeType.NoChange, message: "Not Exported (Based on config)", filename: filename).AsEnumerableOfOne();
                }
            }

            _mutexService.FireItemCompletedEvent(new uSyncExportedItemNotification(attempt.Item, ChangeType.Export));

            return uSyncActionHelper<XElement>.SetAction(attempt, filename, GetItemKey(item), this.Alias).AsEnumerableOfOne();
        }

        #endregion

        #region Reporting 

        public IEnumerable<uSyncAction> Report(string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            var actions = new List<uSyncAction>();

            var cacheKey = PrepCaches();

            callback?.Invoke("Checking Actions", 1, 3);
            actions.AddRange(ReportFolder(folder, config, callback));

            callback?.Invoke("Validating Report", 2, 3);
            actions = ValidateReport(folder, actions);

            CleanCaches(cacheKey);

            callback?.Invoke("Done", 3, 3);
            return actions;
        }

        private List<uSyncAction> ValidateReport(string folder, List<uSyncAction> actions)
        {
            // Alters the existing list, by chaning the type as needed.
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
            foreach (var deleteAction in actions.Where(x => x.Change == ChangeType.Delete))
            {
                // todo: this is only matching by key, but non-tree based serializers also delete by alias.
                // so this check actually has to be booted back down to the serializer.
                if (actions.Any(x => x.Change != ChangeType.Delete && DoActionsMatch(x, deleteAction)))
                {
                    var duplicateAction = uSyncActionHelper<TObject>.ReportActionFail(deleteAction.Name,
                        $"Duplicate! {deleteAction.Name} exists both as delete and import action");

                    // create a detail message to tell people what has happend.
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

            // yes this is an or, we've done it explicity, so you can tell!
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

        public virtual IEnumerable<uSyncAction> ReportFolder(string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            List<uSyncAction> actions = new List<uSyncAction>();


            var files = GetImportFiles(folder);

            int count = 0;
            int total = files.Count();

            logger.LogDebug("ReportFolder: {folder} ({count} files)", folder, total);

            foreach (string file in files)
            {
                count++;
                callback?.Invoke(Path.GetFileNameWithoutExtension(file), count, total);

                actions.AddRange(ReportItem(file, config));
            }

            foreach (var children in syncFileService.GetDirectories(folder))
            {
                actions.AddRange(ReportFolder(children, config, callback));
            }

            return actions;
        }

        protected virtual IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings config)
            => ReportElement(node, filename, config ?? this.DefaultConfig, new uSyncImportOptions());


        /// <summary>
        ///  Report an Element
        /// </summary>
        public IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options)
        {
            try
            {
                // pre event reporting 
                //  this lets us intercept a report and 
                //  shortcut the checking (sometimes).
                if (_mutexService.FireItemStartingEvent(new uSyncReportingItemNotification(node)))
                {
                    return uSyncActionHelper<TObject>
                        .ReportAction(ChangeType.NoChange, node.GetAlias(), GetNameFromFileOrNode(filename, node), node.GetKey(), this.Alias)
                        .AsEnumerableOfOne();
                }

                var actions = new List<uSyncAction>();

                // get the serializer options
                var serializerOptions = new SyncSerializerOptions(options.Flags, settings.Settings);
                serializerOptions.MergeSettings(options.Settings);

                // check if this item is current (the provided xml and exported xml match)
                var change = IsItemCurrent(node, serializerOptions);

                var action = uSyncActionHelper<TObject>
                        .ReportAction(change.Change, node.GetAlias(), GetNameFromFileOrNode(filename, node), node.GetKey(), this.Alias);

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
                        action.Message = "xml is diffrent - but properties may not have changed";
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
                    return uSyncActionHelper<TObject>.ReportAction(false, node.GetAlias(), "Will not be imported (Based on config)")
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
        private bool ShouldProcessEvent()
        {
            if (_mutexService.IsPaused) return false;
            return HandlerActions.Save.IsValidAction(DefaultConfig.Actions);
        }

        public virtual void Handle(DeletedNotification<TObject> notification)
        {
            if (!ShouldProcessEvent()) return;

            foreach (var item in notification.DeletedEntities)
            {
                ExportDeletedItem(item, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);
            }
        }

        public virtual void Handle(SavedNotification<TObject> notification)
        {
            if (!ShouldProcessEvent()) return;

            foreach (var item in notification.SavedEntities)
            {
                var attempts = Export(item, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);

                foreach (var attempt in attempts.Where(x => x.Success))
                {
                    this.CleanUp(item, attempt.FileName, Path.Combine(rootFolder, this.DefaultFolder));
                }
            }
        }

        public virtual void Handle(MovedNotification<TObject> notification)
        {
            if (!ShouldProcessEvent()) return;

            foreach (var item in notification.MoveInfoCollection)
            {
                var attempts = Export(item.Entity, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);

                foreach (var attempt in attempts.Where(x => x.Success))
                {
                    this.CleanUp(item.Entity, attempt.FileName, Path.Combine(rootFolder, this.DefaultFolder));
                }
            }
        }


        protected virtual void ExportDeletedItem(TObject item, string folder, HandlerSettings config)
        {
            if (item == null) return;
            var filename = GetPath(folder, item, config.GuidNames, config.UseFlatStructure);

            var attempt = serializer.SerializeEmpty(item, SyncActionType.Delete, string.Empty);
            if (attempt.Success)
            {
                syncFileService.SaveXElement(attempt.Item, filename);
                this.CleanUp(item, filename, Path.Combine(rootFolder, this.DefaultFolder));
            }
        }

        /// <summary>
        ///  Cleans up the handler folder, removing duplicate configs for this item
        ///  </summary>
        ///  <remarks>
        ///   e.g if someone renames a thing (and we are using the name in the file) 
        ///   this will clean anything else in the folder that has that key / alias
        ///  </remarks>
        /// </summary>
        protected virtual void CleanUp(TObject item, string newFile, string folder)
        {
            var physicalFile = syncFileService.GetAbsPath(newFile);

            var files = syncFileService.GetFiles(folder, "*.config");

            foreach (string file in files)
            {
                // compare the file paths. 
                if (!syncFileService.PathMatches(physicalFile, file)) // This is not the same file, as we are saving.
                {
                    try
                    {
                        var node = syncFileService.LoadXElement(file);

                        // if this xml file matches the item we have just saved. 

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
        // but for blueprints, we want to get diffrent items, (but still use the 
        // content serializer) so we override them.


        protected virtual TObject GetFromService(int id) => serializer.FindItem(id);
        protected virtual TObject GetFromService(Guid key) => serializer.FindItem(key);
        protected virtual TObject GetFromService(string alias) => serializer.FindItem(alias);
        protected virtual void DeleteViaService(TObject item) => serializer.DeleteItem(item);
        protected string GetItemAlias(TObject item) => serializer.ItemAlias(item);
        protected Guid GetItemKey(TObject item) => serializer.ItemKey(item);

        // container ones, only matter when theire is a container?
        // should we bump these up to container 
        abstract protected TObject GetFromService(TContainer item);
        virtual protected TContainer GetContainer(Guid key) => default;
        virtual protected TContainer GetContainer(int id) => default;

        virtual protected string GetItemPath(TObject item, bool useGuid, bool isFlat)
            => useGuid ? GetItemKey(item).ToString() : GetItemAlias(item).ToSafeFileName(shortStringHelper);

        abstract protected string GetItemName(TObject item);

        virtual protected string GetPath(string folder, TObject item, bool GuidNames, bool isFlat)
        {
            if (isFlat && GuidNames) return Path.Combine(folder, $"{GetItemKey(item)}.config");
            var path = Path.Combine(folder, $"{this.GetItemPath(item, GuidNames, isFlat)}.config");

            // if this is flat but not using guid filenames, then we check for clashes.
            if (isFlat && !GuidNames) return CheckAndFixFileClash(path, item);
            return path;
        }


        /// <summary>
        ///  Get a clean filename that doesn't clash with any existing items.
        /// </summary>
        /// <remarks>
        ///  clashes we want to resolve can occur when the safeFilename for an item
        ///  matches with the safe file name for something else. e.g
        ///     1 Special Doctype 
        ///     2 Special Doctype 
        ///     
        ///  Will both resolve to SpecialDocType.Config
        ///  
        ///  the first item to be written to disk for a clash will get the 'normal' name
        ///  all subsequent items will get the appended name. 
        ///  
        ///  this can be completely sidesteped by using guid filenames. 
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
                var append = GetItemKey(item).ToShortKeyString(8); // (this is the shortened guid like media folders do)
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
        ///  clashing aliases at diffrent levels in the folder structure. 
        ///  
        ///  So just checking the alias works, for content we overwrite these two functions.
        /// </remarks>
        protected virtual string GetItemMatchString(TObject item) => GetItemAlias(item);

        protected virtual string GetXmlMatchString(XElement node) => node.GetAlias();

        /// <summary>
        /// Rename an item 
        /// </summary>
        /// <remarks>
        ///  This doesn't get called, because renames generally are handled in the serialization because we match by key.
        /// </remarks>
        virtual public uSyncAction Rename(TObject item) => new uSyncAction();


        public virtual string Group { get; protected set; } = uSyncConstants.Groups.Settings;

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


            return dependencies.DistinctBy(x => x.Udi.ToString()).OrderByDescending(x => x.Order);
        }

        #region Serializer Calls 

        private SyncAttempt<XElement> SerializeItem(TObject item, SyncSerializerOptions options)
            => serializer.Serialize(item, options);

        private SyncAttempt<TObject> DeserializeItem(XElement node, SyncSerializerOptions options)
            => serializer.Deserialize(node, options);

        private SyncAttempt<TObject> DeserializeItemSecondPass(TObject item, XElement node, SyncSerializerOptions options)
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


        #endregion



        private string GetNameFromFileOrNode(string filename, XElement node)
            => !string.IsNullOrWhiteSpace(filename) ? filename : node.GetAlias();


        private string GetCacheKeyBase()
            => $"keycache_{this.Alias}_{Thread.CurrentThread.ManagedThreadId}";

        private string PrepCaches()
        {
            if (this.serializer is ISyncCachedSerializer cachedSerializer)
                cachedSerializer.InitializeCache();

            // make sure the runtime cache is clean.
            var key = GetCacheKeyBase();

            // this also cleares the folder cache - as its a starts with call.
            runtimeCache.ClearByKey(key);
            return key;
        }

        private void CleanCaches(string cacheKey)
        {
            runtimeCache.ClearByKey(cacheKey);

            if (this.serializer is ISyncCachedSerializer cachedSerializer)
                cachedSerializer.DisposeCache();

        }
    }
}
