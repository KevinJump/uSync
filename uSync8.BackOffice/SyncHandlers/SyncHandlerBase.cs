using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.BackOffice.Models;
using uSync8.BackOffice.Services;
using uSync8.Core;
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

        protected readonly uSyncBackOfficeSettings globalSettings;
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
        public uSyncHandlerSettings DefaultConfig { get; set; }

        protected UmbracoObjectTypes itemObjectType = UmbracoObjectTypes.Unknown;
        protected UmbracoObjectTypes itemContainerType = UmbracoObjectTypes.Unknown;

        private string actionFile;

        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            SyncFileService syncFileService,
            uSyncBackOfficeSettings settings)
        {
            this.entityService = entityService;
            this.logger = logger;

            this.globalSettings = settings;
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

            actionFile = Path.Combine(globalSettings.rootFolder, $"{DefaultFolder}_actions.config");
        }

        #region Importing 
        public IEnumerable<uSyncAction> ImportAll(string folder, uSyncHandlerSettings config = null, bool force = false)
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

        protected virtual IEnumerable<uSyncAction> ImportFolder(string folder, uSyncHandlerSettings config, Dictionary<string, TObject> updates, bool force)
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

                actions.Add(uSyncActionHelper<TObject>.SetAction(attempt, file, IsTwoPass));
            }

            var folders = syncFileService.GetDirectories(folder);
            foreach (var children in folders)
            {
                actions.AddRange(ImportFolder(children, config, updates, force));
            }


            return actions;
        }

        virtual public SyncAttempt<TObject> Import(string filePath, uSyncHandlerSettings config, bool force = false)
        {
            syncFileService.EnsureFileExists(filePath);

            using (var stream = syncFileService.OpenRead(filePath))
            {
                var node = XElement.Load(stream);
                var attempt = serializer.Deserialize(node, force, false);
                return attempt;
            }
        }

        virtual public void ImportSecondPass(string file, TObject item, uSyncHandlerSettings config)
        {
            if (IsTwoPass)
            {
                syncFileService.EnsureFileExists(file);

                using (var stream = syncFileService.OpenRead(file))
                {
                    var node = XElement.Load(stream);
                    serializer.DesrtializeSecondPass(item, node);
                    stream.Dispose();
                }
            }
        }

        #endregion

        #region Exporting
        virtual public IEnumerable<uSyncAction> ExportAll(string folder, uSyncHandlerSettings config = null)
        {
            return ExportAll(-1, folder, config);
        }

        virtual public IEnumerable<uSyncAction> ExportAll(int parent, string folder, uSyncHandlerSettings config)
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

            var items = entityService.GetChildren(parent, this.itemObjectType);
            foreach (var item in items)
            {
                var contentType = GetFromService(item.Id);
                actions.Add(Export(contentType, folder, config));

                actions.AddRange(ExportAll(item.Id, folder, config));
            }

            return actions;
        }

        virtual public uSyncAction Export(TObject item, string folder, uSyncHandlerSettings config)
        {
            if (item == null)
                return uSyncAction.Fail(nameof(item), typeof(TObject), ChangeType.Fail, "Item not set");

            var filename = GetPath(folder, item);

            var attempt = serializer.Serialize(item);
            if (attempt.Success)
            {
                syncFileService.SaveXElement(attempt.Item, filename);
            }

            return uSyncActionHelper<XElement>.SetAction(attempt, filename);
        }

        #endregion

        #region reporting 

        public IEnumerable<uSyncAction> Report(string folder, uSyncHandlerSettings config = null)
        {
            var actions = new List<uSyncAction>();
            actions.AddRange(ProcessActions(true));
            actions.AddRange(ReportFolder(folder, config));
            return actions;
        }

        public IEnumerable<uSyncAction> ReportFolder(string folder, uSyncHandlerSettings config)
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

                action.Message = nameof(TObject);

                if (action.Change > ChangeType.NoChange)
                    action.Details = tracker.GetChanges(node);

                return action;
            }
        }

        #endregion

        #region Events 

        protected virtual void ItemDeletedEvent(IService sender, Umbraco.Core.Events.DeleteEventArgs<TObject> e)
        {
            var actionService = new SyncActionService(syncFileService, actionFile);
            actionService.GetActions();

            foreach (var item in e.DeletedEntities)
            {
                DeleteItem(item, Path.Combine(globalSettings.rootFolder, this.DefaultFolder), DefaultConfig);
                actionService.AddAction(item.Key, SyncActionType.Delete);
            }

            actionService.SaveActions();
        }

        protected virtual void ItemSavedEvent(IService sender, Umbraco.Core.Events.SaveEventArgs<TObject> e)
        {
            foreach (var item in e.SavedEntities)
            {
                Export(item, Path.Combine(globalSettings.rootFolder, this.DefaultFolder), DefaultConfig);
            }
        }

        protected virtual void DeleteItem(TObject item, string folder, uSyncHandlerSettings config)
        {
            if (item == null) return;
            var filename = GetPath(folder, item);

            var attempt = serializer.SerializeEmpty(item, GetItemName(item));
            if (attempt.Success)
                syncFileService.SaveXElement(attempt.Item, filename);
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
                            updates.Add(Delete(action.Key, action.Alias, report));
                            break;
                    }
                }
            }

            return updates;
        }
        #endregion

        abstract protected TObject GetFromService(int id);
        abstract protected TObject GetFromService(Guid key);
        abstract protected TObject GetFromService(string alias);
        abstract protected void DeleteViaService(TObject item);

        abstract protected string GetItemPath(TObject item);
        abstract protected string GetItemName(TObject item);

        virtual protected string GetPath(string folder, TObject item)
            => $"{folder}/{this.GetItemPath(item)}.config";

        virtual public uSyncAction Delete(Guid key, string keyString, bool report)
        {
            var item = GetFromService(key);
            if (item == null && !string.IsNullOrWhiteSpace(keyString))
            {
                item = GetFromService(keyString);
            }

            if (item != null)
            {
                if (!report) DeleteViaService(item);

                return uSyncAction.SetAction(true, item.Key.ToString(), typeof(TObject), ChangeType.Delete);
            }

            return uSyncAction.SetAction(false, keyString, typeof(TObject), ChangeType.Delete);
        }

        virtual public uSyncAction Rename(TObject item)
            => new uSyncAction();
    }
}
