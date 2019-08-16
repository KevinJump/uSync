using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Models;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;
using static Umbraco.Core.Constants;
using static uSync8.BackOffice.uSyncService;

namespace uSync8.BackOffice.SyncHandlers
{
    public abstract class SyncHandlerBase<TObject, TService>
        where TObject : IEntity
        where TService : IService
    {
        protected readonly IProfilingLogger logger;
        protected readonly IEntityService entityService;

        protected readonly SyncFileService syncFileService;

        protected readonly ISyncDependencyChecker<TObject> dependencyChecker;

        protected readonly ISyncSerializer<TObject> serializer;
        protected readonly ISyncTracker<TObject> tracker;

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
        private UmbracoObjectTypes ItemObjectType { get; set; } = UmbracoObjectTypes.Unknown;
        private UmbracoObjectTypes itemContainerType = UmbracoObjectTypes.Unknown;

        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            SyncFileService syncFileService)
        : this(entityService, logger, serializer, tracker, null, syncFileService) { }


        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            ISyncDependencyChecker<TObject> dependencyChecker,
            SyncFileService syncFileService)
        {
            this.logger = logger;

            this.entityService = entityService;

            this.serializer = serializer;
            this.tracker = tracker;
            this.dependencyChecker = dependencyChecker;

            this.syncFileService = syncFileService;

            var thisType = GetType();
            var meta = thisType.GetCustomAttribute<SyncHandlerAttribute>(false);
            if (meta == null)
                throw new InvalidOperationException($"The Handler {thisType} requires a {typeof(SyncHandlerAttribute)}");

            Name = meta.Name;
            Alias = meta.Alias;
            DefaultFolder = meta.Folder;
            Priority = meta.Priority;
            IsTwoPass = meta.IsTwoPass;
            Icon = string.IsNullOrWhiteSpace(meta.Icon) ? "icon-umb-content" : meta.Icon;
            EntityType = meta.EntityType;

            TypeName = serializer.ItemType;

            this.ItemObjectType = uSyncObjectType.ToUmbracoObjectType(EntityType);
            this.itemContainerType = uSyncObjectType.ToContainerUmbracoObjectType(EntityType);

            GetDefaultConfig(Current.Configs.uSync());
            uSyncConfig.Reloaded += BackOfficeConfig_Reloaded;
        }

        private void GetDefaultConfig(uSyncSettings setting)
        {
            var config = setting.DefaultHandlerSet().Handlers.Where(x => x.Alias.InvariantEquals(this.Alias))
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
            logger.Info<uSync8BackOffice>("Running Import: {0}", Path.GetFileName(folder));

            var actions = new List<uSyncAction>();
            var updates = new Dictionary<string, TObject>();

            actions.AddRange(ImportFolder(folder, config, updates, force, callback));

            if (updates.Any())
            {
                foreach (var item in updates.Select((update, Index) => new { update, Index }))
                {
                    callback?.Invoke($"Second Pass {Path.GetFileName(item.update.Key)}", item.Index, updates.Count);
                    ImportSecondPass(item.update.Key, item.update.Value, config, callback);
                }
            }

            callback?.Invoke("Done", 1, 1);
            return actions;
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
                callback?.Invoke($"Saving {updates.Count()} changes", 1, 1);
                serializer.Save(updates.Select(x => x.Value));
            }

            var folders = syncFileService.GetDirectories(folder);
            foreach (var children in folders)
            {
                actions.AddRange(ImportFolder(children, config, updates, force, callback));
            }

            if (actions.All(x => x.Success))
            {
                foreach (var cleanFile in cleanMarkers)
                {
                    actions.AddRange(CleanFolder(cleanFile, false));
                }
            }

            callback?.Invoke("", 1, 1);

            return actions;
        }


        /// <summary>
        ///  given a folder we calculate what items we can remove, becuase they are 
        ///  not in one the the files in the folder.
        /// </summary>
        /// <param name="cleanFile"></param>
        /// <returns></returns>
        protected virtual IEnumerable<uSyncAction> CleanFolder(string cleanFile, bool reportOnly)
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
            var keys = new List<Guid>();
            var files = syncFileService.GetFiles(folder, "*.config");
            foreach (var file in files)
            {
                var node = XElement.Load(file);
                var key = node.GetKey();
                if (!keys.Contains(key))
                    keys.Add(key);
            }

            return DeleteMissingItems(parent, keys, reportOnly);
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
                    var actualItem = GetFromService(item.Key);
                    var name = actualItem.Id;

                    if (!reportOnly)
                        DeleteViaService(actualItem);

                    actions.Add(uSyncActionHelper<TObject>.SetAction(SyncAttempt<TObject>.Succeed(name.ToString(), ChangeType.Delete), string.Empty));
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
                syncFileService.EnsureFileExists(filePath);

                using (var stream = syncFileService.OpenRead(filePath))
                {
                    var node = XElement.Load(stream);
                    var attempt = serializer.Deserialize(node, flags);
                    return attempt;
                }
            }
            catch (FileNotFoundException notFoundException)
            {
                return SyncAttempt<TObject>.Fail(Path.GetFileName(filePath), ChangeType.Fail, $"File not found {notFoundException.Message}");
            }
            catch (Exception ex)
            {
                return SyncAttempt<TObject>.Fail(Path.GetFileName(filePath), ChangeType.Fail, $"Import Fail: {ex.Message}");
            }
        }

        virtual public void ImportSecondPass(string file, TObject item, HandlerSettings config, SyncUpdateCallback callback)
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
                        serializer.DeserializeSecondPass(item, node, flags);
                        stream.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn<TObject>($"Second Import Failed: {ex.Message}");
                }
            }
        }

        #endregion

        #region Exporting
        virtual public IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            // we dont clean the folder out on an export all. 
            // because the actions (renames/deletes) live in the folder
            //
            // there will have to be a diffrent clean option
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

            callback?.Invoke("Done", 1, 1);
            return actions;
        }

        // almost everything does this - but languages can't so we need to 
        // let the language Handler override this. 
        virtual protected IEnumerable<IEntity> GetChildItems(int parent)
        {
            if (this.ItemObjectType != UmbracoObjectTypes.Unknown)
                return entityService.GetChildren(parent, this.ItemObjectType);

            return Enumerable.Empty<IEntity>();
        }


        virtual protected IEnumerable<IEntitySlim> GetFolders(int parent)
        {
            if (this.itemContainerType != UmbracoObjectTypes.Unknown)
                return entityService.GetChildren(parent, this.itemContainerType);

            return Enumerable.Empty<IEntitySlim>();
        }


        public bool HasChildren(int id)
            => GetFolders(id).Any() || GetChildItems(id).Any();

        virtual public IEnumerable<uSyncAction> Export(TObject item, string folder, HandlerSettings config)
        {
            if (item == null)
                return uSyncAction.Fail(nameof(item), typeof(TObject), ChangeType.Fail, "Item not set").AsEnumerableOfOne();

            var filename = GetPath(folder, item, config.GuidNames, config.UseFlatStructure);

            var attempt = serializer.Serialize(item);
            if (attempt.Success)
            {
                syncFileService.SaveXElement(attempt.Item, filename);
            }

            return uSyncActionHelper<XElement>.SetAction(attempt, filename).AsEnumerableOfOne();
        }

        #endregion

        #region reporting 

        public IEnumerable<uSyncAction> Report(string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            var actions = new List<uSyncAction>();
            callback?.Invoke("Checking Actions", 0, 1);
            actions.AddRange(ReportFolder(folder, config, callback));
            callback?.Invoke("Done", 1, 1);
            return actions;
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

                actions.AddRange(ReportItem(file));
            }

            foreach (var children in syncFileService.GetDirectories(folder))
            {
                actions.AddRange(ReportFolder(children, config, callback));
            }


            return actions;
        }

        public IEnumerable<uSyncAction> ReportElement(XElement node)
            => ReportElement(node, string.Empty);

        private IEnumerable<uSyncAction> ReportElement(XElement node, string filename)
        {

            try
            {
                var actions = new List<uSyncAction>();

                var change = serializer.IsCurrent(node);
                var action = uSyncActionHelper<TObject>
                        .ReportAction(change, node.GetAlias(), !string.IsNullOrWhiteSpace(filename) ? filename : node.GetAlias(), node.GetKey(), this.Alias);

                action.Message = "";

                switch (action.Change)
                {
                    case ChangeType.Clean:
                        actions.AddRange(CleanFolder(filename, true));
                        break;
                    case ChangeType.Delete:
                        action.Details = tracker.GetChanges(node);
                        if (action.Details == null || action.Details.Count() == 0)
                        {
                            action.Message = "Change details cannot be calculated";
                        }

                        action.Message = $"{action.Change.ToString()}";
                        break;
                }

                actions.Add(action);
                return actions;
            }
            catch (FormatException fex)
            {
                return uSyncActionHelper<TObject>
                    .ReportActionFail(Path.GetFileName(node.GetAlias()), $"format error {fex.Message}")
                    .AsEnumerableOfOne();
            }
        }

        protected IEnumerable<uSyncAction> ReportItem(string file)
        {
            try
            {
                var node = syncFileService.LoadXElement(file);
                return ReportElement(node, file);
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

                foreach (var attempt in attempts.Where(x => x.Success))
                {
                    this.CleanUp(item, attempt.FileName, Path.Combine(rootFolder, this.DefaultFolder));
                }
            }
        }

        protected virtual void EventMovedItem(IService sender, MoveEventArgs<TObject> e)
        {
            if (uSync8BackOffice.eventsPaused) return;

            foreach (var item in e.MoveInfoCollection)
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
                syncFileService.SaveXElement(attempt.Item, filename);
        }

        /// <summary>
        ///  cleans up the folder, so if someone renames a things
        ///  (and we are using the name in the file) this will
        ///  clean anything else in the folder that has that key
        /// </summary>
        protected void CleanUp(TObject item, string newFile, string folder)
        {
            var physicalFile = syncFileService.GetAbsPath(newFile);

            var files = syncFileService.GetFiles(folder, "*.config");

            foreach (string file in files)
            {
                if (!file.InvariantEquals(physicalFile))
                {
                    var node = syncFileService.LoadXElement(file);
                    if (node.GetKey() == item.Key)
                    {
                        var attempt = serializer.SerializeEmpty(item, SyncActionType.Rename, node.GetAlias());
                        if (attempt.Success)
                        {
                            syncFileService.SaveXElement(attempt.Item, file);
                        }
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

        virtual protected string GetPath(string folder, TObject item, bool GuidNames, bool isFlat)
        {
            if (isFlat && GuidNames) return $"{folder}/{item.Key}.config";

            return $"{folder}/{this.GetItemPath(item, GuidNames, isFlat)}.config";
        }

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

            var attempt = serializer.Deserialize(node, flags);
            return uSyncActionHelper<TObject>.SetAction(attempt, node.GetAlias(), this.Alias, IsTwoPass)
                .AsEnumerableOfOne();
        }

        public IEnumerable<uSyncAction> Report(string file, HandlerSettings config)
            => ReportItem(file);


        public IEnumerable<uSyncAction> Export(int id, string folder, HandlerSettings settings)
        {
            var item = this.GetFromService(id);
            return this.Export(item, folder, settings);
        }

        public IEnumerable<uSyncAction> Export(Udi udi, string folder, HandlerSettings settings)
        {
            if (udi is GuidUdi guidUdi)
            {
                var item = this.GetFromService(guidUdi.Guid);
                if (item != null)
                    return Export(item, folder, settings);
            }

            return uSyncAction.Fail(nameof(udi), typeof(TObject), ChangeType.Fail, "Item not found")
                .AsEnumerableOfOne();
        }

        public SyncAttempt<XElement> GetElement(Udi udi)
        {
            if (udi is GuidUdi guidUdi)
            {
                var element = this.GetFromService(guidUdi.Guid);
                if (element == null)
                {
                    var entity = entityService.Get(guidUdi.Guid);
                    if (entity != null)
                        element = GetFromService(entity.Id);
                }

                if (element != null)
                    return this.serializer.Serialize(element);
            }

            return SyncAttempt<XElement>.Fail(udi.ToString(), ChangeType.Fail);
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
            if (item == null) return Enumerable.Empty<uSyncDependency>();

            logger.Info<uSync8Core>("Found Item {0}", item.Id);
            return dependencyChecker.GetDependencies(item, flags);

        }

        private IEnumerable<uSyncDependency> GetContainerDependencies(int id, DependencyFlags flags)
        {
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
                        dependencies.AddRange(dependencyChecker.GetDependencies(childItem, flags));
                    }
                }
            }


            return dependencies.DistinctBy(x => x.Udi.ToString()).OrderByDescending(x => x.Order);
        }

        #endregion

    }
}
