using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.BackOffice.SyncHandlers
{
    public abstract class SyncHandlerRoot<TObject, TContainer>
    {
        protected readonly IProfilingLogger logger;

        protected readonly SyncFileService syncFileService;

        protected readonly IList<ISyncDependencyChecker<TObject>> dependencyCheckers;
        protected readonly IList<ISyncTracker<TObject>> trackers;

        // [Obsolete]
        // protected ISyncDependencyChecker<TObject> dependencyChecker => dependencyCheckers.FirstOrDefault();

        protected readonly ISyncSerializer<TObject> serializer;


        // [Obsolete]
        // protected ISyncTracker<TObject> tracker => trackers.FirstOrDefault();

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
        public Type ItemType { get; protected set; } = typeof(TObject);

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

        protected Type handlerType;

        protected readonly ISyncItemFactory itemFactory;

        [Obsolete("Use constructors with collections")]
        protected SyncHandlerRoot(
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<TObject> serializer,
            IEnumerable<ISyncTracker<TObject>> trackers,
            IEnumerable<ISyncDependencyChecker<TObject>> checkers,
            SyncFileService syncFileService)
            : this(logger, appCaches, serializer, null, syncFileService)
        { }

        public SyncHandlerRoot(
                IProfilingLogger logger,
                AppCaches appCaches,
                ISyncSerializer<TObject> serializer,
                ISyncItemFactory itemFactory,
                SyncFileService syncFileService)
        {
            this.logger = logger;
            this.itemFactory = itemFactory ?? Current.Factory.GetInstance<ISyncItemFactory>();

            this.serializer = serializer;
            this.trackers = this.itemFactory.GetTrackers<TObject>().ToList();
            this.dependencyCheckers = this.itemFactory.GetCheckers<TObject>().ToList();

            this.syncFileService = syncFileService;

            handlerType = GetType();
            var meta = handlerType.GetCustomAttribute<SyncHandlerAttribute>(false);
            if (meta == null)
                throw new InvalidOperationException($"The Handler {handlerType} requires a {typeof(SyncHandlerAttribute)}");

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

            var settings = Current.Configs.uSync();
            GetDefaultConfig(settings);
            uSyncConfig.Reloaded += BackOfficeConfig_Reloaded;

            if (settings.CacheFolderKeys)
            {
                this.runtimeCache = appCaches.RuntimeCache;
            }
            else
            {
                logger.Info(handlerType, "No caching of handler key lookups (CacheFolderKeys = false)");
                this.runtimeCache = NoAppCache.Instance;
            }
        }

        private void GetDefaultConfig(uSyncSettings setting)
        {
            var config = setting.DefaultHandlerSet()?.Handlers.Where(x => x.Alias.InvariantEquals(this.Alias))
                .FirstOrDefault();

            if (config != null)
                this.DefaultConfig = config;
            else
            {
                // handler isn't in the config, but need one ?
                this.DefaultConfig = new HandlerSettings(this.Alias, false)
                {
                    GuidNames = new OverriddenValue<bool>(setting.UseGuidNames, false),
                    UseFlatStructure = new OverriddenValue<bool>(setting.UseFlatStructure, false),
                };
            }

            rootFolder = setting.RootFolder;
        }

        private void BackOfficeConfig_Reloaded(uSyncSettings settings)
        {
            GetDefaultConfig(settings);
        }

        #region Importing 

        /// <summary>
        ///  Import everything from a given folder, using the supplied config settings.
        /// </summary>
        public IEnumerable<uSyncAction> ImportAll(string folder, HandlerSettings config, bool force, SyncUpdateCallback callback = null)
        {
            using (logger.DebugDuration(handlerType, $"Importing {Alias} {Path.GetFileName(folder)}", $"Import complete {Alias}"))
            {
                var actions = new List<uSyncAction>();
                var updates = new Dictionary<string, TObject>();

                var cacheKey = GetCacheKeyBase();

                logger.Debug(handlerType, "Clearing KeyCache {key}", cacheKey);
                runtimeCache.ClearByKey(cacheKey);

                actions.AddRange(ImportFolder(folder, config, updates, force, callback));

                if (updates.Count > 0)
                {
                    PerformSecondPassImports(updates, actions, config, callback);
                }

                logger.Debug(handlerType, "Clearing KeyCache {key}", cacheKey);
                runtimeCache.ClearByKey(cacheKey);
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

                var attempt = Import(file, config, flags);
                if (attempt.Success)
                {
                    if (attempt.Change == ChangeType.Clean)
                    {
                        cleanMarkers.Add(file);
                    }
                    else if (attempt.Item != null)
                    {
                        updates.Add(file, attempt.Item);
                    }
                }

                var action = uSyncActionHelper<TObject>.SetAction(attempt, file, this.Alias, IsTwoPass);
                if (attempt.Details != null && attempt.Details.Any())
                    action.Details = attempt.Details;

                if (attempt.Change != ChangeType.Clean)
                    actions.Add(action);
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
        public virtual SyncAttempt<TObject> Import(string filePath, HandlerSettings config, SerializerFlags flags)
        {
            try
            {
                if (config.FailOnMissingParent) flags |= SerializerFlags.FailMissingParent;

                syncFileService.EnsureFileExists(filePath);
                using (var stream = syncFileService.OpenRead(filePath))
                {
                    var node = XElement.Load(stream);
                    if (ShouldImport(node, config))
                    {
                        var attempt = DeserializeItem(node, new SyncSerializerOptions(flags, config.Settings));
                        return attempt;
                    }
                    else
                    {
                        return SyncAttempt<TObject>.Succeed(Path.GetFileName(filePath),
                            ChangeType.NoChange, "Not Imported (Based on config)");
                    }
                }
            }
            catch (FileNotFoundException notFoundException)
            {
                return SyncAttempt<TObject>.Fail(Path.GetFileName(filePath), ChangeType.Fail, $"File not found {notFoundException.Message}");
            }
            catch (Exception ex)
            {
                logger.Warn(handlerType, "{alias}: Import Failed : {exception}", this.Alias, ex.ToString());
                return SyncAttempt<TObject>.Fail(Path.GetFileName(filePath), ChangeType.Fail, $"Import Fail: {ex.Message}");
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

                    using (var stream = syncFileService.OpenRead(file))
                    {
                        var node = XElement.Load(stream);
                        var attempt = DeserializeItemSecondPass(item, node, new SyncSerializerOptions(flags, config.Settings));
                        stream.Dispose();
                        return attempt;
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(handlerType, $"Second Import Failed: {ex.ToString()}");
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
            if (!flat)
            {
                // when the folder isn't flat we don't want the parent folder, but rather the folder of the children
                // of this item.
                var cleanFileFolder = Path.GetFileNameWithoutExtension(cleanFile);
                if (cleanFileFolder.IndexOf('_') >= 0)
                {
                    cleanFileFolder = cleanFileFolder.Substring(0, cleanFileFolder.LastIndexOf('_'));
                    folder = Path.Combine(folder, cleanFileFolder);
                }

                logger.Debug(handlerType, "Non Flat Folder {folder}", folder);
            }

            if (!Directory.Exists(folder)) return Enumerable.Empty<uSyncAction>();

            var parent = GetCleanParent(cleanFile);
            if (parent == null) return Enumerable.Empty<uSyncAction>();

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
                // keys should aways have at least one entry (the key from cleanFile)
                // if it doesn't then something might have gone wrong.
                // because we are being defensive when it comes to deletes, 
                // we only then do deletes when we know we have loaded some keys!
                return DeleteMissingItems(parent, keys, reportOnly);
            }
            else
            {
                logger.Warn(handlerType, "Failed to get the keys for items in the folder, there might be a disk issue {folder}", folder);
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

            return runtimeCache.GetCacheItem(cacheKey, () =>
            {
                var keys = new List<Guid>();
                var files = syncFileService.GetFiles(folder, "*.config").ToList();

                foreach (var file in files)
                {
                    var node = XElement.Load(file);
                    var key = node.GetKey();
                    if (!keys.Contains(key))
                    {
                        keys.Add(key);
                    }
                }

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
            => syncFileService.GetFiles(folder, "*.config");

        /// <summary>
        ///  check to see if this element should be imported as part of the process.
        /// </summary>
        virtual protected bool ShouldImport(XElement node, HandlerSettings config)
        {
            // if createOnly is on, then we only create things that are not already there. 
            // this lookup is slow(ish) so we only do it if we have to.
            if (config.GetSetting<bool>(uSyncConstants.DefaultSettings.CreateOnly, uSyncConstants.DefaultSettings.CreateOnly_Default)
                || config.GetSetting<bool>(uSyncConstants.DefaultSettings.OneWay, uSyncConstants.DefaultSettings.CreateOnly_Default))
            {
                var item = serializer.FindItem(node);
                if (item != null)
                {
                    logger.Debug(handlerType, "CreateOnly: Item {alias} already exist not importing it.", node.GetAlias());
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
                var concreateType = GetFromService(item.Value);
                callback?.Invoke(GetItemName(concreateType), item.Index, items.Count);

                actions.AddRange(Export(concreateType, folder, config));
                actions.AddRange(ExportAll(item.Value, folder, config, callback));
            }

            return actions;
        }

        abstract protected IEnumerable<TContainer> GetChildItems(TContainer parent);
        abstract protected IEnumerable<TContainer> GetFolders(TContainer parent);

        public bool HasChildren(TContainer item)
            => GetFolders(item).Any() || GetChildItems(item).Any();

        virtual public IEnumerable<uSyncAction> Export(TObject item, string folder, HandlerSettings config)
        {
            if (item == null)
                return uSyncAction.Fail(nameof(item), typeof(TObject), ChangeType.Fail, "Item not set").AsEnumerableOfOne();

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
                    return uSyncAction.SetAction(true, filename, type: typeof(TObject), change: ChangeType.NoChange, message: "Not Exported (Based on config)", filename: filename).AsEnumerableOfOne();
                }
            }

            return uSyncActionHelper<XElement>.SetAction(attempt, filename).AsEnumerableOfOne();
        }

        #endregion

        #region Reporting 

        public IEnumerable<uSyncAction> Report(string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            var actions = new List<uSyncAction>();

            var cacheKey = GetCacheKeyBase();

            runtimeCache.ClearByKey(cacheKey);

            callback?.Invoke("Checking Actions", 1, 3);
            actions.AddRange(ReportFolder(folder, config, callback));

            callback?.Invoke("Validating Report", 2, 3);
            actions = ValidateReport(folder, actions);

            runtimeCache.ClearByKey(cacheKey);

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
                        logger.Debug(handlerType, "Found existing key in actions {item}", actions[i].Name);
                        actions[i].Change = ChangeType.Create;
                    }
                    else
                    {
                        logger.Warn(handlerType, "{item} is missing a parent", actions[i].Name);
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

            logger.Debug(handlerType, "ReportFolder: {folder} ({count} files)", folder, total);

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

        public IEnumerable<uSyncAction> ReportElement(XElement node)
            => ReportElement(node, string.Empty, null);

        protected virtual IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings config)
        {
            try
            {
                var actions = new List<uSyncAction>();
                var serializerOptions = new SyncSerializerOptions(config.Settings);

                var change = IsItemCurrent(node, serializerOptions);
                var action = uSyncActionHelper<TObject>
                        .ReportAction(change, node.GetAlias(), !string.IsNullOrWhiteSpace(filename) ? filename : node.GetAlias(), node.GetKey(), this.Alias);

                action.Message = "";

                if (action.Change == ChangeType.Clean)
                {
                    actions.AddRange(CleanFolder(filename, true, config.UseFlatStructure));
                }
                else if (action.Change > ChangeType.NoChange)
                {
                    action.Details = GetChanges(node, serializerOptions);
                    if (action.Change != ChangeType.Create && (action.Details == null || action.Details.Count() == 0))
                    {
                        action.Message = "xml is diffrent - but properties may not have changed";
                        action.Details = MakeRawChange(node, serializerOptions).AsEnumerableOfOne();
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

                return actions;
            }
            catch (FormatException fex)
            {
                return uSyncActionHelper<TObject>
                    .ReportActionFail(Path.GetFileName(node.GetAlias()), $"format error {fex.Message}")
                    .AsEnumerableOfOne();
            }
        }

        private uSyncChange MakeRawChange(XElement node, SyncSerializerOptions options)
        {
            var item = this.GetFromService(node.GetKey());
            var currentNode = GetCurrent(item, options);
            if (currentNode.Success)
            {
                return uSyncChange.Update(node.GetAlias(), "Raw XML", currentNode.Item.ToString(), node.ToString());
            }

            return uSyncChange.NoChange(node.GetAlias(), node.GetAlias());

        }


        private SyncAttempt<XElement> GetCurrent(TObject item, SyncSerializerOptions options)
        {
            if (item != null)
            {
                if (serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                    return optionSerializer.Serialize(item, options);

#pragma warning disable CS0618 // Type or member is obsolete
                return serializer.Serialize(item);
#pragma warning restore CS0618 // Type or member is obsolete

            }

            return SyncAttempt<XElement>.Fail("unknown", ChangeType.Fail);

        }

        protected IEnumerable<uSyncAction> ReportItem(string file, HandlerSettings config)
        {
            try
            {
                logger.Debug(handlerType, "Report Item {file}", file);

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
                    .ReportActionFail(Path.GetFileName(file), $"Reporing error {ex.Message}")
                    .AsEnumerableOfOne();
            }

        }


        private IEnumerable<uSyncChange> GetChanges(XElement node, SyncSerializerOptions options)
            => itemFactory.GetChanges<TObject>(node, options);

        #endregion

        #region Events 

        // 
        // Handling the events Umbraco fires for saves/deletes/etc, 
        //  For most things these events are all handled the same way, so the root handler can copy with 
        //  it. If the events need to be handled a diffrent way, then that is done inside the Handler
        //  by overriding the InitializeEvents method. 
        //

        /// <summary>
        ///  Method to setup the events for any given service/handler.
        /// </summary>
        protected abstract void InitializeEvents(HandlerSettings settings);


        protected virtual void EventDeletedItem(IService sender, DeleteEventArgs<TObject> e)
        {
            if (uSync8BackOffice.eventsPaused) return;
            foreach (var item in e.DeletedEntities)
            {
                ExportDeletedItem(item, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);
            }
        }

        protected virtual void EventSavedItem(IService sender, SaveEventArgs<TObject> e)
        {
            if (uSync8BackOffice.eventsPaused) return;

            foreach (var item in e.SavedEntities)
            {
                var attempts = Export(item, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);

                // if we are using guid names and a flat structure then the clean doesn't need to happen
                if (!(this.DefaultConfig.GuidNames && this.DefaultConfig.UseFlatStructure))
                {
                    foreach (var attempt in attempts.Where(x => x.Success))
                    {
                        this.CleanUp(item, attempt.FileName, Path.Combine(rootFolder, this.DefaultFolder));
                    }
                }
            }
        }

        protected virtual void EventMovedItem(IService sender, MoveEventArgs<TObject> e)
        {
            if (uSync8BackOffice.eventsPaused) return;

            foreach (var item in e.MoveInfoCollection)
            {
                var attempts = Export(item.Entity, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);

                if (!(this.DefaultConfig.GuidNames && this.DefaultConfig.UseFlatStructure))
                {
                    foreach (var attempt in attempts.Where(x => x.Success))
                    {
                        this.CleanUp(item.Entity, attempt.FileName, Path.Combine(rootFolder, this.DefaultFolder));
                    }
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
                                logger.Debug(handlerType, "Duplicate {file} of {alias}, saving as rename", Path.GetFileName(file), this.GetItemAlias(item));

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
                        logger.Warn(handlerType, "Error during cleanup of existing files {message}", ex.Message);
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

        abstract protected TObject GetFromService(int id);
        abstract protected TObject GetFromService(Guid key);
        abstract protected TObject GetFromService(string alias);
        abstract protected TObject GetFromService(TContainer item);
        abstract protected void DeleteViaService(TObject item);

        virtual protected TContainer GetContainer(Guid key) => default;
        virtual protected TContainer GetContainer(int id) => default;

        abstract protected string GetItemPath(TObject item, bool useGuid, bool isFlat);
        abstract protected string GetItemName(TObject item);

        virtual protected string GetItemAlias(TObject item)
            => GetItemName(item);

        virtual protected string GetPath(string folder, TObject item, bool GuidNames, bool isFlat)
        {
            if (isFlat && GuidNames) return Path.Combine(folder, $"{GetItemKey(item)}.config");
            var path = Path.Combine(folder, $"{this.GetItemPath(item, GuidNames, isFlat)}.config");

            // if this is flat but not using guid filenames, then we check for clashes.
            if (isFlat && !GuidNames) return CheckAndFixFileClash(path, item);
            return path;
        }

        abstract protected Guid GetItemKey(TObject item);

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


        /// <summary>
        ///  Setup any events or other things we need to do when the event handler is started.
        /// </summary>
        public void Initialize(HandlerSettings settings)
        {
            InitializeEvents(settings);
        }

        #region ISyncHandler2 Methods 

        public virtual string Group { get; protected set; } = uSyncBackOfficeConstants.Groups.Settings;

        virtual public IEnumerable<uSyncAction> Import(string file, HandlerSettings config, bool force)
        {
            var flags = SerializerFlags.OnePass;
            if (force) flags |= SerializerFlags.Force;

            var attempt = Import(file, config, flags);
            return uSyncActionHelper<TObject>.SetAction(attempt, file, this.Alias, IsTwoPass)
                .AsEnumerableOfOne();
        }

        virtual public IEnumerable<uSyncAction> ImportElement(XElement node, bool force)
        {
            var flags = SerializerFlags.OnePass;
            if (force) flags |= SerializerFlags.Force;

            var attempt = DeserializeItem(node, new SyncSerializerOptions(flags));
            return uSyncActionHelper<TObject>.SetAction(attempt, node.GetAlias(), this.Alias, IsTwoPass)
                .AsEnumerableOfOne();
        }

        public IEnumerable<uSyncAction> Report(string file, HandlerSettings config)
            => ReportItem(file, config);


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

            return uSyncAction.Fail(nameof(udi), typeof(TObject), ChangeType.Fail, "Item not found")
                .AsEnumerableOfOne();
        }

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

        #endregion


        #region Serializer Calls 

#pragma warning disable CS0618 // Type or member is obsolete

        private SyncAttempt<XElement> SerializeItem(TObject item, SyncSerializerOptions options)
        {
            if (serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                return optionSerializer.Serialize(item, options);

            return serializer.Serialize(item);
        }

        private SyncAttempt<TObject> DeserializeItem(XElement node, SyncSerializerOptions options)
        {
            if (serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                return optionSerializer.Deserialize(node, options);

            return serializer.Deserialize(node, options.Flags);
        }

        private SyncAttempt<TObject> DeserializeItemSecondPass(TObject item, XElement node, SyncSerializerOptions options)
        {
            if (serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                return optionSerializer.DeserializeSecondPass(item, node, options);

            return serializer.DeserializeSecondPass(item, node, options.Flags);
        }

        private ChangeType IsItemCurrent(XElement node, SyncSerializerOptions options)
        {
            if (serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                return optionSerializer.IsCurrent(node, options);

            return serializer.IsCurrent(node);
        }

#pragma warning restore CS0618 // Type or member is obsolete

        #endregion


        private string GetCacheKeyBase()
            => $"keycache_{this.Alias}_{Thread.CurrentThread.ManagedThreadId}";

    }
}
