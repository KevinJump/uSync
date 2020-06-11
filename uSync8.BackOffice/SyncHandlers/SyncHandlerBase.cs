using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
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
    public abstract class SyncHandlerBase<TObject, TService>
        where TObject : IEntity
        where TService : IService
    {
        protected readonly IProfilingLogger logger;
        protected readonly IEntityService entityService;

        protected readonly SyncFileService syncFileService;

        protected readonly IList<ISyncDependencyChecker<TObject>> checkers;

        protected readonly ISyncSerializer<TObject> serializer;
        protected readonly ISyncTracker<TObject> tracker;
        protected readonly IAppPolicyCache runtimeCache;

        // handler things 
        public string Alias { get; private set; }
        public string Name { get; private set; }
        public string DefaultFolder { get; private set; }
        public int Priority { get; private set; }
        public string Icon { get; private set; }

        protected bool IsTwoPass = false;

        public Type ItemType { get; protected set; } = typeof(TObject);

        /// settings can be loaded for these.
        public bool Enabled { get; set; } = true;
        public HandlerSettings DefaultConfig { get; set; }

        protected string rootFolder { get; set; }

        public string EntityType { get; protected set; }

        public string TypeName { get; protected set; }

        // we calculate these now based on the entityType ? 
        private UmbracoObjectTypes itemObjectType { get; set; } = UmbracoObjectTypes.Unknown;

        private UmbracoObjectTypes itemContainerType = UmbracoObjectTypes.Unknown;

        protected Type handlerType;

        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            SyncFileService syncFileService)
        : this(entityService, logger, serializer, tracker, appCaches, Enumerable.Empty<ISyncDependencyChecker<TObject>>(), syncFileService) { }

        [Obsolete("Construct your handler using SyncDependencyCollection for better checker support")]
        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            ISyncDependencyChecker<TObject> dependencyChecker,
            SyncFileService syncFileService)
        : this(entityService, logger, serializer, tracker, appCaches, dependencyChecker.AsEnumerableOfOne(), syncFileService)
        { }


        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            SyncDependencyCollection checkers,
            SyncFileService syncFileService)
            : this(entityService, logger, serializer, tracker, appCaches, checkers.GetCheckers<TObject>(), syncFileService)
        { }

        public SyncHandlerBase(IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            IEnumerable<ISyncDependencyChecker<TObject>> checkers,
            SyncFileService syncFileService)
        {


            this.logger = logger;

            this.entityService = entityService;

            this.serializer = serializer;
            this.tracker = tracker;
            this.checkers = checkers.ToList();

            this.syncFileService = syncFileService;

            this.runtimeCache = appCaches.RuntimeCache;

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

            GetDefaultConfig(Current.Configs.uSync());
            uSyncConfig.Reloaded += BackOfficeConfig_Reloaded;
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
        public IEnumerable<uSyncAction> ImportAll(string folder, HandlerSettings config, bool force, SyncUpdateCallback callback = null)
        {
            var sw = Stopwatch.StartNew();
            logger.Debug(handlerType, "{alias} ImportAll: {fileName}", this.Alias, Path.GetFileName(folder));

            var actions = new List<uSyncAction>();
            var updates = new Dictionary<string, TObject>();

            runtimeCache.ClearByKey($"keycache_{this.Alias}");

            actions.AddRange(ImportFolder(folder, config, updates, force, callback));

            if (updates.Any())
            {
                ProcessSecondPasses(updates, actions, config, callback);
            }

            runtimeCache.ClearByKey($"keycache_{this.Alias}");
            callback?.Invoke("Done", 3, 3);

            sw.Stop();
            logger.Debug(handlerType, "{alias} Import Complete {elapsedMilliseconds}ms", this.Alias, sw.ElapsedMilliseconds);
            return actions;
        }

        private void ProcessSecondPasses(IDictionary<string, TObject> updates, List<uSyncAction> actions, HandlerSettings config, SyncUpdateCallback callback = null)
        {
            List<TObject> updatedItems = new List<TObject>();
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

                    if (attempt.Change > ChangeType.NoChange)
                    {
                        updatedItems.Add(attempt.Item);
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

            if (config.BatchSave)
            {
                callback?.Invoke($"Saving {updatedItems.Count} Second Pass Items", 2, 3);
                serializer.Save(updatedItems);
            }

        }

        protected virtual IEnumerable<uSyncAction> ImportFolder(string folder, HandlerSettings config, Dictionary<string, TObject> updates, bool force, SyncUpdateCallback callback)
        {
            List<uSyncAction> actions = new List<uSyncAction>();
            var files = GetImportFiles(folder);

            var flags = SerializerFlags.None;
            if (force) flags |= SerializerFlags.Force;
            if (config.BatchSave) flags |= SerializerFlags.DoNotSave;

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
                // this is just extra messaging, given how quickly the next message will be sent.
                // callback?.Invoke("Cleaning Folders", 1, cleanMarkers.Count);

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
        ///  given a folder we calculate what items we can remove, becuase they are 
        ///  not in one the the files in the folder.
        /// </summary>
        /// <param name="cleanFile"></param>
        /// <returns></returns>
        protected virtual IEnumerable<uSyncAction> CleanFolder(string cleanFile, bool reportOnly, bool flat)
        {
            var folder = Path.GetDirectoryName(cleanFile);

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

            return DeleteMissingItems(parent, keys, reportOnly);
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

            var cacheKey = $"keycache_{this.Alias}_{folderKey}";
            return runtimeCache.GetCacheItem(cacheKey, () =>
            {
                var keys = new List<Guid>();
                var files = syncFileService.GetFiles(folder, "*.config");
                foreach (var file in files)
                {
                    var node = XElement.Load(file);
                    var key = node.GetKey();
                    if (!keys.Contains(key))
                        keys.Add(key);
                }

                return keys;

            }, null);
        }

        protected TObject GetCleanParent(string file)
        {
            var node = XElement.Load(file);
            var key = node.GetKey();
            if (key == Guid.Empty) return default;

            return GetFromService(key);
        }

        protected IEnumerable<uSyncAction> DeleteMissingItems(TObject parent, IEnumerable<Guid> keys, bool reportOnly)
        {
            var items = GetChildItems(parent.Id).ToList();
            var actions = new List<uSyncAction>();
            foreach (var item in items)
            {
                if (!keys.Contains(item.Key))
                {
                    var name = String.Empty;
                    if (item is IEntitySlim slim) name = slim.Name;
                    if (string.IsNullOrEmpty(name) || !reportOnly)
                    {
                        var actualItem = GetFromService(item.Key);
                        name = GetItemName(actualItem);

                        // actually do the delete if we are really not reporting
                        if (!reportOnly) DeleteViaService(actualItem);
                    }

                    // for reporting - we use the entity name,
                    // this stops an extra lookup - which we may not need later
                    actions.Add(uSyncActionHelper<TObject>.SetAction(SyncAttempt<TObject>.Succeed(name, ChangeType.Delete), string.Empty));
                }
            }

            return actions;
        }


        protected virtual IEnumerable<string> GetImportFiles(string folder)
            => syncFileService.GetFiles(folder, "*.config");


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
                        return SyncAttempt<TObject>.Succeed(Path.GetFileName(filePath), default(TObject), ChangeType.NoChange, "Not Imported (Based on config)");
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
        ///  check to see if this element should be imported as part of the process.
        /// </summary>
        virtual protected bool ShouldImport(XElement node, HandlerSettings config) => true;

        /// <summary>
        ///  Check to see if this elment should be exported. 
        /// </summary>
        virtual protected bool ShouldExport(XElement node, HandlerSettings config) => true;

        virtual public SyncAttempt<TObject> ImportSecondPass(string file, TObject item, HandlerSettings config, SyncUpdateCallback callback)
        {
            if (IsTwoPass)
            {
                try
                {
                    syncFileService.EnsureFileExists(file);

                    var flags = SerializerFlags.None;
                    if (config.BatchSave)
                        flags |= SerializerFlags.DoNotSave;

                    using (var stream = syncFileService.OpenRead(file))
                    {
                        var node = XElement.Load(stream);
                        var attempt = DeserializeItemSecondPass(item, node, new SyncSerializerOptions(flags));
                        stream.Dispose();
                        return attempt;
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(handlerType, $"Second Import Failed: {ex.ToString()}");
                    return SyncAttempt<TObject>.Fail(item.Id.ToString(), ChangeType.Fail, ex.Message, ex);
                }
            }

            return SyncAttempt<TObject>.Succeed(item.Id.ToString(), ChangeType.NoChange);
        }

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

            return ExportAll(-1, folder, config, callback);
        }

        virtual public IEnumerable<uSyncAction> ExportAll(int parent, string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            var actions = new List<uSyncAction>();

            if (itemContainerType != UmbracoObjectTypes.Unknown)
            {
                var containers = GetFolders(parent);
                foreach (var container in containers)
                {
                    actions.AddRange(ExportAll(container.Id, folder, config, callback));
                }
            }

            var items = GetChildItems(parent).ToList();
            foreach (var item in items.Select((Value, Index) => new { Value, Index }))
            {
                var concreateType = GetFromService(item.Value.Id);
                callback?.Invoke(GetItemName(concreateType), item.Index, items.Count);

                actions.AddRange(Export(concreateType, folder, config));
                actions.AddRange(ExportAll(item.Value.Id, folder, config, callback));
            }

            // callback?.Invoke("Done", 1, 1);
            return actions;
        }

        // almost everything does this - but languages can't so we need to 
        // let the language Handler override this. 
        virtual protected IEnumerable<IEntity> GetChildItems(int parent)
        {
            if (this.itemObjectType != UmbracoObjectTypes.Unknown)
                return entityService.GetChildren(parent, this.itemObjectType);

            return Enumerable.Empty<IEntity>();
        }


        virtual protected IEnumerable<IEntity> GetFolders(int parent)
        {
            if (this.itemContainerType != UmbracoObjectTypes.Unknown)
                return entityService.GetChildren(parent, this.itemContainerType);

            return Enumerable.Empty<IEntity>();
        }


        public bool HasChildren(int id)
            => GetFolders(id).Any() || GetChildItems(id).Any();

        virtual public IEnumerable<uSyncAction> Export(TObject item, string folder, HandlerSettings config)
        {
            if (item == null)
                return uSyncAction.Fail(nameof(item), typeof(TObject), ChangeType.Fail, "Item not set").AsEnumerableOfOne();

            var filename = GetPath(folder, item, config.GuidNames, config.UseFlatStructure);

            var attempt = this.SerializeItem(item, new SyncSerializerOptions(config.Settings));
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

            runtimeCache.ClearByKey($"keycache_{this.Alias}");

            callback?.Invoke("Checking Actions", 1, 3);
            actions.AddRange(ReportFolder(folder, config, callback));

            callback?.Invoke("Validating Report", 2, 3);
            actions = ValidateReport(folder, actions);

            runtimeCache.ClearByKey($"keycache_{this.Alias}");

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

                    details.Add(uSyncChange.Delete(
                        Path.GetFileName(deleteAction.FileName),
                        $"Delete: {deleteAction.Name} ({Path.GetFileName(deleteAction.FileName)}",
                        deleteAction.FileName.Substring(folder.Length)));

                    // add all the duplicates to the list of changes.
                    foreach (var dup in actions.Where(x => x.Change != ChangeType.Delete && DoActionsMatch(x, deleteAction)))
                    {

                        details.Add(uSyncChange.Update(
                            Path.GetFileName(dup.FileName),
                            $"{dup.Change}: {dup.Name} ({Path.GetFileName(dup.FileName)}",
                            "",
                            dup.FileName.Substring(folder.Length)));
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
            if (item.Key == node.GetKey()) return true;

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

        /// <summary>
        ///  Report the diffrences between an XML Representation of an item, and what is inside Umbraco.
        /// </summary>
        protected virtual IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings config)
        {
            try
            {
                var actions = new List<uSyncAction>();

                var change = this.IsItemCurrent(node, new SyncSerializerOptions(config.Settings));
                var action = uSyncActionHelper<TObject>
                        .ReportAction(change, node.GetAlias(), !string.IsNullOrWhiteSpace(filename) ? filename : node.GetAlias(), node.GetKey(), this.Alias);

                action.Message = "";

                if (action.Change == ChangeType.Clean)
                {
                    actions.AddRange(CleanFolder(filename, true, config.UseFlatStructure));
                }
                else if (action.Change > ChangeType.NoChange)
                {
                    action.Details = tracker.GetChanges(node);
                    if (action.Change != ChangeType.Create && (action.Details == null || action.Details.Count() == 0))
                    {
                        action.Message = "Change details not calculated";
                    }
                    else
                    {
                        action.Message = $"{action.Change.ToString()}";
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
                    .ReportActionFail(Path.GetFileName(file), $"Reporing error {ex.Message}")
                    .AsEnumerableOfOne();
            }

        }

        #endregion

        #region Events 

        protected virtual void EventDeletedItem(IService sender, Umbraco.Core.Events.DeleteEventArgs<TObject> e)
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
        abstract protected void DeleteViaService(TObject item);

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

        virtual protected Guid GetItemKey(TObject item) => item.Key;

        /// <summary>
        ///  clashes we want to resolve can only occur, when the 
        ///  items can be called the same but in be in different places (e.g content, media).
        /// </summary>
        /// <param name="path"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        virtual protected string CheckAndFixFileClash(string path, TObject item)
            => path;

        virtual public uSyncAction Rename(TObject item)
            => new uSyncAction();


        public void Initialize(HandlerSettings settings)
        {
            InitializeEvents(settings);
        }

        protected abstract void InitializeEvents(HandlerSettings settings);


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
            var item = this.GetFromService(key);
            return GetDependencies(item, flags);
        }

        public IEnumerable<uSyncDependency> GetDependencies(int id, DependencyFlags flags)
        {
            var item = this.GetFromService(id);
            if (item == null) return GetContainerDependencies(id, flags);
            return GetDependencies(item, flags);
        }

        protected IEnumerable<uSyncDependency> GetDependencies(TObject item, DependencyFlags flags)
        {
            if (checkers == null || checkers.Count == 0) return Enumerable.Empty<uSyncDependency>();
            if (item == null) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();
            foreach (var checker in checkers)
            {
                dependencies.AddRange(checker.GetDependencies(item, flags));
            }
            return dependencies;
        }

        private IEnumerable<uSyncDependency> GetContainerDependencies(int id, DependencyFlags flags)
        {
            if (checkers == null || checkers.Count == 0) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            var containers = GetFolders(id);
            if (containers != null && containers.Any())
            {
                foreach (var container in containers)
                {
                    dependencies.AddRange(GetContainerDependencies(container.Id, flags));
                }
            }

            var children = GetChildItems(id);
            if (children != null && children.Any())
            {
                foreach (var child in children)
                {
                    var childItem = GetFromService(child.Id);
                    if (childItem != null)
                    {
                        dependencies.AddRange(GetDependencies(childItem, flags));
                    }
                }
            }


            return dependencies.DistinctBy(x => x.Udi.ToString()).OrderByDescending(x => x.Order);
        }

        #endregion

#pragma warning disable 0618
        //
        // Seperated out the calls to the serializer where we might use the Options - the options
        // allow us to pass things to the serializer that change how/what is serialized.
        // 

        /// <summary>
        ///  Call the serializer to deserialize this item (based on the type of serializer we have)
        /// </summary>
        private SyncAttempt<TObject> DeserializeItem(XElement node, SyncSerializerOptions options)
        {
            if (serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                return optionSerializer.Deserialize(node, options);

            return serializer.Deserialize(node, options.Flags);
        }

        private ChangeType IsItemCurrent(XElement node, SyncSerializerOptions options)
        {
            if (serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                return optionSerializer.IsCurrent(node, options);

            return serializer.IsCurrent(node);
        }

        private SyncAttempt<TObject> DeserializeItemSecondPass(TObject item, XElement node, SyncSerializerOptions options)
        {
            if (serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                return optionSerializer.DeserializeSecondPass(item, node, options);
            
            return serializer.DeserializeSecondPass(item, node, options.Flags);
        }

        private SyncAttempt<XElement> SerializeItem(TObject item, SyncSerializerOptions options)
        {
            if (serializer is ISyncOptionsSerializer<TObject> optionSerializer)
                return optionSerializer.Serialize(item, options);

            return serializer.Serialize(item);

        }
#pragma warning restore 0618

    }
}

