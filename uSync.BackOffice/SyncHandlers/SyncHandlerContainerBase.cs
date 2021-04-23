using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Serialization;

namespace uSync.BackOffice.SyncHandlers
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
            IShortStringHelper shortStringHelper,
            ILogger<SyncHandlerContainerBase<TObject, TService>> logger,
            uSyncConfigService uSyncConfig,
            AppCaches appCaches,
            ISyncSerializer<TObject> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService,
            IEntityService entityService)
            : base(shortStringHelper, logger, uSyncConfig, appCaches, serializer, syncItemFactory, syncFileService, entityService)
        { }

        protected IEnumerable<uSyncAction> CleanFolders(string folder, int parent)
        {
            var actions = new List<uSyncAction>();
            var folders = GetFolders(parent);
            foreach (var fdlr in folders)
            {
                actions.AddRange(CleanFolders(folder, fdlr.Id));

                if (!HasChildren(fdlr))
                {
                    // get the name (from the slim)
                    var name = fdlr.Id.ToString();
                    if (fdlr is IEntitySlim slim)
                    {
                        name = slim.Name;
                    }

                    actions.Add(uSyncAction.SetAction(true, name, typeof(EntityContainer).ToString(), ChangeType.Delete, "Empty Container"));
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

        public virtual void Handle(EntityContainerSavedNotification notification)
        { 
            if (uSyncBackOffice.eventsPaused) return;

            foreach(var folder in notification.SavedEntities)
            {
                if (folder.ContainedObjectType == this.itemObjectType.GetGuid())
                {
                    UpdateFolder(folder.Id, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);
                }
            }
        }
    }
}
