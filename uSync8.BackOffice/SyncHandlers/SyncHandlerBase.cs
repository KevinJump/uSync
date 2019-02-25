using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Models;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;
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

        protected UmbracoObjectTypes itemObjectType = UmbracoObjectTypes.Unknown;
        protected UmbracoObjectTypes itemContainerType = UmbracoObjectTypes.Unknown;

        private string actionFile;

        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            SyncFileService syncFileService)
        {
            this.logger = logger;


            this.entityService = entityService;

            this.serializer = serializer;
            this.tracker = tracker;

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


            GetDefaultConfig(Current.Configs.uSync());
            uSyncConfig.Reloaded += BackOfficeConfig_Reloaded;
        }

        private void GetDefaultConfig(uSyncSettings setting)
        {
            var config = setting.Handlers.FirstOrDefault(x => x.Alias == this.Alias);
            if (config != null)
                this.DefaultConfig = config;
            else
            {
                // handler isn't in the config, but need one ?
                this.DefaultConfig = new HandlerSettings(this.Alias, setting.EnableMissingHandlers)
                {
                    GuidNames = new OverriddenValue<bool>(setting.UseGuidNames, false),
                    UseFlatStructure = new OverriddenValue<bool>(setting.UseFlatStructure, false),
                };
            }

            rootFolder = setting.RootFolder;

            actionFile = Path.Combine(rootFolder, $"_Actions/actions_{DefaultFolder}.config");
        }

        private void BackOfficeConfig_Reloaded(uSyncSettings settings)
        {
            GetDefaultConfig(settings);
        }

        #region Importing 
        public IEnumerable<uSyncAction> ImportAll(string folder, HandlerSettings config = null, bool force = false)
        {
            logger.Info<uSync8BackOffice>("Running Import: {0}", Path.GetFileName(folder));

            var actions = new List<uSyncAction>();
            var updates = new Dictionary<string, TObject>();

            actions.AddRange(ProcessActions(false));
            actions.AddRange(ImportFolder(folder, config, updates, force));

            if (updates.Any())
            {
                foreach (var update in updates)
                {
                    ImportSecondPass(update.Key, update.Value, config);
                }
            }

            return actions;
        }

        protected virtual IEnumerable<uSyncAction> ImportFolder(string folder, HandlerSettings config, Dictionary<string, TObject> updates, bool force)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var files = syncFileService.GetFiles(folder, "*.config");

            foreach (string file in files)
            {
                var attempt = Import(file, config, force);
                if (attempt.Success && attempt.Item != null)
                {
                    updates.Add(file, attempt.Item);
                }

                var action = uSyncActionHelper<TObject>.SetAction(attempt, file, IsTwoPass);
                if (attempt.Details != null && attempt.Details.Any())
                    action.Details = attempt.Details;

                actions.Add(action);
            }

            var folders = syncFileService.GetDirectories(folder);
            foreach (var children in folders)
            {
                actions.AddRange(ImportFolder(children, config, updates, force));
            }


            return actions;
        }

        virtual public SyncAttempt<TObject> Import(string filePath, HandlerSettings config, bool force = false)
        {
            syncFileService.EnsureFileExists(filePath);

            using (var stream = syncFileService.OpenRead(filePath))
            {
                var node = XElement.Load(stream);
                var attempt = serializer.Deserialize(node, force, false);
                return attempt;
            }
        }

        virtual public void ImportSecondPass(string file, TObject item, HandlerSettings config)
        {
            if (IsTwoPass)
            {
                syncFileService.EnsureFileExists(file);

                using (var stream = syncFileService.OpenRead(file))
                {
                    var node = XElement.Load(stream);
                    serializer.DeserializeSecondPass(item, node);
                    stream.Dispose();
                }
            }
        }

        #endregion

        #region Exporting
        virtual public IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings config = null)
        {
            // we clean the folder out on an export all. 
            syncFileService.CleanFolder(folder);

            return ExportAll(-1, folder, config);
        }

        virtual public IEnumerable<uSyncAction> ExportAll(int parent, string folder, HandlerSettings config)
        {
            var actions = new List<uSyncAction>();

            if (itemContainerType != UmbracoObjectTypes.Unknown)
            {
                var containers = entityService.GetChildren(parent, this.itemContainerType);
                foreach (var container in containers)
                {
                    actions.AddRange(ExportAll(container.Id, folder, config));
                }
            }

            var items = GetExportItems(parent, itemObjectType);
            foreach (var item in items)
            {
                var concreateType = GetFromService(item.Id);
                actions.Add(Export(concreateType, folder, config));

                actions.AddRange(ExportAll(item.Id, folder, config));
            }

            return actions;
        }

        // almost everything does this - but languages can't so we need to 
        // let the language Handler override this. 
        virtual protected IEnumerable<IEntity> GetExportItems(int parent, UmbracoObjectTypes objectType)
            => entityService.GetChildren(parent, objectType);

        virtual public uSyncAction Export(TObject item, string folder, HandlerSettings config)
        {
            if (item == null)
                return uSyncAction.Fail(nameof(item), typeof(TObject), ChangeType.Fail, "Item not set");

            var filename = GetPath(folder, item, config.GuidNames, config.UseFlatStructure);

            var attempt = serializer.Serialize(item);
            if (attempt.Success)
            {
                syncFileService.SaveXElement(attempt.Item, filename);
            }

            return uSyncActionHelper<XElement>.SetAction(attempt, filename);
        }

        #endregion

        #region reporting 

        public IEnumerable<uSyncAction> Report(string folder, HandlerSettings config = null)
        {
            var actions = new List<uSyncAction>();
            actions.AddRange(ProcessActions(true));
            actions.AddRange(ReportFolder(folder, config));
            return actions;
        }

        public IEnumerable<uSyncAction> ReportFolder(string folder, HandlerSettings config)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var files = syncFileService.GetFiles(folder, "*.config");

            foreach (string file in files)
            {
                actions.Add(ReportItem(file));
            }

            foreach (var children in syncFileService.GetDirectories(folder))
            {
                actions.AddRange(ReportFolder(children, config));
            }


            return actions;
        }

        protected uSyncAction ReportItem(string file)
        {
            var node = syncFileService.LoadXElement(file);
            if (node.IsEmptyItem())
            {
                return uSyncAction.SetAction(true, node.GetAlias(), typeof(TObject), ChangeType.Removed, "Deleted Item");
            }
            else
            {
                var current = serializer.IsCurrent(node);

                var action = uSyncActionHelper<TObject>
                    .ReportAction(!current, node.GetAlias());

                action.Message = "";

                if (action.Change > ChangeType.NoChange)
                {
                    action.Details = tracker.GetChanges(node);
                    if (action.Details == null || action.Details.Count() == 0)
                    {
                        action.Message = "Change details cannot be calculated";
                    }

                    action.Message = "Would update";
                }

                return action;
            }
        }

        #endregion

        #region Events 

        protected virtual void EventDeletedItem(IService sender, Umbraco.Core.Events.DeleteEventArgs<TObject> e)
        {
            if (uSync8BackOffice.eventsPaused) return;

            var actionService = new SyncActionService(syncFileService, actionFile);
            actionService.GetActions();

            foreach (var item in e.DeletedEntities)
            {
                ExportDeletedItem(item, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);
                actionService.AddAction(item.Key, GetItemName(item), SyncActionType.Delete);
            }

            actionService.SaveActions();
        }

        protected virtual void EventSavedItem(IService sender, Umbraco.Core.Events.SaveEventArgs<TObject> e)
        {
            if (uSync8BackOffice.eventsPaused) return;

            var actionService = new SyncActionService(syncFileService, actionFile);
            actionService.GetActions();

            foreach (var item in e.SavedEntities)
            {
                var attempt = Export(item, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);
                if (attempt.Success)
                {
                    if (!DefaultConfig.GuidNames)
                    {
                        actionService.CleanActions(item.Key, GetItemName(item));
                    }
                }
            }
            actionService.SaveActions();
        }

        protected virtual void ExportDeletedItem(TObject item, string folder, HandlerSettings config)
        {
            if (item == null) return;
            var filename = GetPath(folder, item, config.GuidNames, config.UseFlatStructure);

            var attempt = serializer.SerializeEmpty(item, GetItemName(item));
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
            var files = syncFileService.GetFiles(folder, "*.config");

            foreach (string file in files)
            {
                if (!file.InvariantEquals(newFile)) {
                    var node = syncFileService.LoadXElement(file);
                    if (node.GetKey() == item.Key)
                    {
                        var attempt = serializer.SerializeEmpty(item, GetItemName(item));
                        if (attempt.Success)
                            attempt.Item.Save(file);
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

        #region Actions


        protected IEnumerable<uSyncAction> ProcessActions(bool report)
        {
            var updates = new List<uSyncAction>();

            var actionService = new SyncActionService(syncFileService, actionFile);
            var actions = actionService.GetActions();

            if (actions != null && actions.Any())
            {
                foreach (var action in actions)
                {
                    switch (action.Action)
                    {
                        case SyncActionType.Delete:
                            updates.Add(ProcessDelete(action.Key, action.Alias, report));
                            break;
                    }
                }
            }

            return updates;
        }

        virtual public uSyncAction ProcessDelete(Guid key, string keyString, bool report)
        {
            var item = GetFromService(key);
            if (item == null && !string.IsNullOrWhiteSpace(keyString))
            {
                item = GetFromService(keyString);
            }

            if (item != null)
            {
                var message = "";
                if (!report)
                {
                    DeleteViaService(item);
                }
                else
                {
                    message = "Would be deleted";
                }
                return uSyncAction.SetAction(true, keyString, typeof(TObject), ChangeType.Delete, message);
            }

            return uSyncAction.SetAction(false, keyString, typeof(TObject), ChangeType.Removed);
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

    }
}
