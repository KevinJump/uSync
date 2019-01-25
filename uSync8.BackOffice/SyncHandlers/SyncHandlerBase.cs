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
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Serialization;

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

        public string Alias { get; private set; }
        public string Name { get; private set; }
        public string DefaultFolder { get; private set; }
        public int Priority { get; private set; }
        public bool Enabled { get; protected set; } = true;
        protected bool IsTwoPass = false;

        protected UmbracoObjectTypes itemObjectType = UmbracoObjectTypes.Unknown;
        protected UmbracoObjectTypes itemContainerType = UmbracoObjectTypes.Unknown;

        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            SyncFileService syncFileService,
            uSyncBackOfficeSettings settings)
        {
            this.entityService = entityService;
            this.logger = logger;

            this.globalSettings = settings;
            this.serializer = serializer;
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

        }

        #region Importing 
        public IEnumerable<uSyncAction> ImportAll(string folder, bool force)
        {
            logger.Info<uSync8BackOffice>("Running Import: {0}", Path.GetFileName(folder));

            var actions = new List<uSyncAction>();
            var updates = new Dictionary<string, TObject>();

            actions.AddRange(ProcessActions());
            actions.AddRange(ImportFolder(folder, force, updates));

            if (updates.Any())
            {
                foreach (var update in updates)
                {
                    ImportSecondPass(update.Key, update.Value);
                }
            }

            return actions;
        }

        private IEnumerable<uSyncAction> ImportFolder(string folder, bool force, Dictionary<string, TObject> updates)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var files = syncFileService.GetFiles(folder, "*.config");

            foreach (string file in files)
            {
                var attempt = Import(file, force);
                if (attempt.Success && attempt.Item != null)
                {
                    updates.Add(file, attempt.Item);
                }

                actions.Add(uSyncActionHelper<TObject>.SetAction(attempt, file, IsTwoPass));
            }

            var folders = syncFileService.GetDirectories(folder);

            foreach (var children in folders)
            {
                actions.AddRange(ImportFolder(children, force, updates));
            }


            return actions;
        }

        virtual public SyncAttempt<TObject> Import(string filePath, bool force = false)
        {
            syncFileService.EnsureFileExists(filePath);

            using (var stream = syncFileService.OpenRead(filePath))
            {
                var node = XElement.Load(stream);
                var attempt = serializer.Deserialize(node, force, false);
                return attempt;
                stream.Dispose();
            }
        }

        virtual public void ImportSecondPass(string file, TObject item)
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
        virtual public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            return ExportAll(-1, folder);
        }

        virtual public IEnumerable<uSyncAction> ExportAll(int parent, string folder)
        {
            var actions = new List<uSyncAction>();

            if (itemContainerType != UmbracoObjectTypes.Unknown)
            {
                var containers = entityService.GetChildren(parent, this.itemContainerType);
                foreach (var container in containers)
                {
                    actions.AddRange(ExportAll(container.Id, folder));
                }
            }

            var items = entityService.GetChildren(parent, this.itemObjectType);
            foreach (var item in items)
            {
                var contentType = GetFromService(item.Id);
                actions.Add(Export(contentType, folder));

                actions.AddRange(ExportAll(item.Id, folder));
            }

            return actions;
        }

        virtual public uSyncAction Export(TObject item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail(nameof(item), typeof(TObject), ChangeType.Fail, "Item not set");

            var filename = GetPath(folder, item);

            var attempt = serializer.Serialize(item);
            if (attempt.Success)
            {
                using (var stream = syncFileService.OpenWrite(filename))
                {
                    attempt.Item.Save(stream);
                    stream.Flush();
                    stream.Dispose();
                }
            }

            return uSyncActionHelper<XElement>.SetAction(attempt, filename);
        }

        #endregion

        #region reporting 

        public IEnumerable<uSyncAction> Report(string folder)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var files = syncFileService.GetFiles(folder, "*.config");

            foreach (string file in files)
            {
                actions.Add(ReportItem(file));
            }

            foreach (var children in syncFileService.GetDirectories(folder))
            {
                actions.AddRange(Report(children));
            }


            return actions;
        }

        #endregion

        #region Events 

        protected virtual void ItemDeletedEvent(IService sender, Umbraco.Core.Events.DeleteEventArgs<TObject> e)
        {
            // throw new NotImplementedException();
        }

        protected virtual void ItemSavedEvent(IService sender, Umbraco.Core.Events.SaveEventArgs<TObject> e)
        {
            foreach (var item in e.SavedEntities)
            {
                Export(item, this.DefaultFolder);
            }
        }

        #endregion


        abstract protected TObject GetFromService(int id);
        abstract public uSyncAction ReportItem(string file);

        virtual protected string GetPath(string folder, TObject item)
        {
            return $"{folder}/{this.GetItemPath(item)}.config";
        }

        abstract protected string GetItemPath(TObject item);

        protected IEnumerable<uSyncAction> ProcessActions()
        {
            return Enumerable.Empty<uSyncAction>();
        }

        virtual public uSyncAction Delete(Guid key, string keyString)
            => new uSyncAction();

        virtual public uSyncAction Rename(TObject item)
            => new uSyncAction();
    }
}
