using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Core.Cache;
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
    /// alias in different containers. 
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
            AppCaches appCaches,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, appCaches, syncFileService)
        { }

        protected SyncHandlerContainerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            SyncTrackerCollection trackers,
            AppCaches appCaches,
            SyncDependencyCollection checkers,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, trackers, appCaches, checkers, syncFileService)
        {
        }

        [Obsolete("Construct your handler using SyncDependencyCollection for better checker support")]
        protected SyncHandlerContainerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            ISyncDependencyChecker<TObject> checker,
            SyncFileService fileService)
            : base(entityService, logger, serializer, tracker, appCaches, checker, fileService)
        {

        }


        protected IEnumerable<uSyncAction> CleanFolders(string folder, int parent)
        {
            var actions = new List<uSyncAction>();
            var folders = GetFolders(parent);
            foreach (var fdlr in folders)
            {
                actions.AddRange(CleanFolders(folder, fdlr.Id));

                if (!HasChildren(fdlr.Id))
                {
                    // get the name (from the slim)
                    var name = fdlr.Id.ToString();
                    if (fdlr is IEntitySlim slim) {
                        name = slim.Name;
                    }

                    actions.Add(uSyncAction.SetAction(true, name, typeof(EntityContainer), ChangeType.Delete, "Empty Container"));
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
            var folders = GetFolders(folderId);
            foreach (var fdlr in folders)
            {
                actions.AddRange(UpdateFolder(fdlr.Id, folder, config));
            }

            var items = GetChildItems(folderId);
            foreach (var item in items)
            {
                var obj = GetFromService(item.Id);
                if (obj != null)
                {
                    var attempts = Export(obj, folder, config);
                    foreach (var attempt in attempts.Where(x => x.Success))
                    {
                        // when its flat structure and use guidNames, we don't need to cleanup.
                        if (!(this.DefaultConfig.GuidNames && this.DefaultConfig.UseFlatStructure))
                        {
                            CleanUp(obj, attempt.FileName, folder);
                        }

                        actions.Add(attempt);
                    }
                }
            }

            return actions;

        }

        protected void EventContainerSaved(IService service, SaveEventArgs<EntityContainer> e)
        {
            if (uSync8BackOffice.eventsPaused) return;

            foreach (var folder in e.SavedEntities)
            {
                UpdateFolder(folder.Id, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);
            }
        }
    }
}
