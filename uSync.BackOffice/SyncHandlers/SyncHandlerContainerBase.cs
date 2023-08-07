using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
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

        /// <inheritdoc/>
        protected SyncHandlerContainerBase(
            ILogger<SyncHandlerContainerBase<TObject, TService>> logger,
            IEntityService entityService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
        { }

        /// <summary>
        ///  Removes any empty 'containers' after import 
        /// </summary>
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

                    actions.Add(uSyncAction.SetAction(true, name, typeof(EntityContainer).Name, ChangeType.Delete, "Empty Container"));
                    DeleteFolder(fdlr.Id);
                }
            }

            return actions;
        }


        /// <summary>
        /// delete a container
        /// </summary>
        abstract protected void DeleteFolder(int id);

        /// <summary>
        /// Handle events at the end of any import 
        /// </summary>
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
            if (this.serializer is SyncContainerSerializerBase<TObject> containerSerializer)
            {
                containerSerializer.InitializeCache();
            }

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

        /// <summary>
        ///  Handle container saving events
        /// </summary>
        /// <param name="notification"></param>
        public virtual void Handle(EntityContainerSavedNotification notification)
        {
            if (_mutexService.IsPaused) return;

            ProcessContainerChanges(notification.SavedEntities);
        }

        public virtual void Handle(EntityContainerRenamedNotification notification)
        {
            if (_mutexService.IsPaused) return;
            ProcessContainerChanges(notification.Entities);
        }

        private void ProcessContainerChanges(IEnumerable<EntityContainer> containers)
        {
            foreach (var folder in containers)
            {
                if (folder.ContainedObjectType == this.itemObjectType.GetGuid())
                {
                    UpdateFolder(folder.Id, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);
                }
            }
        }

        /// <summary>
        ///  Does this item match the one in a given xml file? 
        /// </summary>
        /// <remarks>
        ///  container based tree's aren't really trees - as in things can't have the same 
        ///  name inside a folder as something else that might be outside the folder.
        ///  
        ///  this means when we are comparing files for clean up, we also want to check the 
        ///  alias. so we check the key (in the base) and if doesn't match we check the alias.
        ///  
        ///  under default setup none of this matters because the name of the item is the file
        ///  name so we find/overwrite it anyway, 
        ///  
        ///  but for guid / folder structured setups we need to do this compare. 
        /// </remarks>
        protected override bool DoItemsMatch(XElement node, TObject item)
        {
            if (base.DoItemsMatch(node, item)) return true;
            return node.GetAlias().InvariantEquals(GetItemAlias(item));
        }
    }
}
