using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.BackOffice.SyncHandlers
{
    /// <summary>
    ///  Base hanlder for objects that have container based trees
    /// </summary>
    /// <remarks>
    /// <para>
    /// In container based trees all items have unique aliases 
    /// across the whole tree. 
    /// </para>
    /// <para>
    /// you can't for example have two doctypes with the same 
    /// alias in diffrent containers. 
    /// </para>
    /// </remarks>
    public abstract class SyncHandlerContainerBase<TObject, TService>
        : SyncHandlerTreeBase<TObject, TService>
        where TObject : ITreeEntity
        where TService : IService
    {
        protected SyncHandlerContainerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, syncFileService)
        { }

        protected SyncHandlerContainerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            ISyncDependencyChecker<TObject> checker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, checker, syncFileService)
        {
        }

        protected IEnumerable<uSyncAction> CleanFolders(string folder, int parent)
        {
            var actions = new List<uSyncAction>();
            var folders = entityService.GetChildren(parent, this.itemContainerType);
            foreach (var fdlr in folders)
            {
                actions.AddRange(CleanFolders(folder, fdlr.Id));

                if (!entityService.GetChildren(fdlr.Id, this.ItemObjectType).Any()
                    && !entityService.GetChildren(fdlr.Id, this.itemContainerType).Any())
                {
                    actions.Add(uSyncAction.SetAction(true, fdlr.Name, typeof(EntityContainer), ChangeType.Delete, "Empty Container"));
                    DeleteFolder(fdlr.Id);
                }
            }

            return actions;
        }

        abstract protected void DeleteFolder(int id);

        public virtual IEnumerable<uSyncAction> ProcessPostImport(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
        {
            if (actions == null || !actions.Any())
                return null;

            return CleanFolders(folder, -1);
        }

        /// <summary>
        ///  will resave everything in a folder (and beneath)
        ///  we need to this when it's renamed
        /// </summary>
        /// <param name="folderId"></param>
        /// <param name="folder"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        protected IEnumerable<uSyncAction> UpdateFolder(int folderId, string folder, HandlerSettings config)
        {
            var actions = new List<uSyncAction>();
            var folders = entityService.GetChildren(folderId, this.itemContainerType);
            foreach (var fdlr in folders)
            {
                actions.AddRange(UpdateFolder(fdlr.Id, folder, config));
            }

            var items = entityService.GetChildren(folderId, this.ItemObjectType);
            foreach(var item in items)
            {
                var obj = GetFromService(item.Id);
                if (obj != null)
                {
                    var attempt = Export(obj, folder, config);
                    if (attempt.Success)
                    {
                        CleanUp(obj, attempt.FileName, folder);
                    }
                    actions.Add(attempt);
                }
            }

            return actions;

        }

        protected void EventContainerSaved(IService service, SaveEventArgs<EntityContainer> e)
        {
            if (uSync8BackOffice.eventsPaused) return;

            foreach(var folder in e.SavedEntities)
            {
                UpdateFolder(folder.Id, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);
            }
        }
    }
}
