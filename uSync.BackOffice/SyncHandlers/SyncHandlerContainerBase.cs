using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Serialization;

namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    ///  Base handler for objects that have container based trees
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
        [Obsolete("We don't need to pass the folder. will be removed in v15")]
        protected IEnumerable<uSyncAction> CleanFolders(string folder, int parent)
            => CleanFolders(parent);

        /// <summary>
        ///  Removes any empty 'containers' after import 
        /// </summary>
        protected IEnumerable<uSyncAction> CleanFolders(int parent)
        {
            var actions = new List<uSyncAction>();
            var folders = GetChildItems(parent, this.itemContainerType);
            foreach (var fdlr in folders)
            {
                logger.LogDebug("Checking Container: {folder} for any childItems [{type}]", fdlr.Id, fdlr?.GetType()?.Name ?? "Unknown");
                actions.AddRange(CleanFolders(fdlr.Id));

                if (!HasChildren(fdlr))
                {
                    // get the name (from the slim)
                    var name = fdlr.Id.ToString();
                    if (fdlr is IEntitySlim slim)
                    {
                        // if this item isn't an container type, don't delete. 
                        if (ObjectTypes.GetUmbracoObjectType(slim.NodeObjectType) != this.itemContainerType) continue;

                        name = slim.Name;
                        logger.LogDebug("Folder has no children {name} {type}", name, slim.NodeObjectType);
                    }

                    actions.Add(uSyncAction.SetAction(true, name, typeof(EntityContainer).Name, ChangeType.Delete, "Empty Container"));
                    DeleteFolder(fdlr.Id);
                }
            }

            return actions;
        }

        private bool IsContainer(Guid guid)
            => guid == Constants.ObjectTypes.DataTypeContainer
            || guid == Constants.ObjectTypes.MediaTypeContainer
            || guid == Constants.ObjectTypes.DocumentTypeContainer;

        /// <summary>
        /// delete a container
        /// </summary>
        abstract protected void DeleteFolder(int id);

        /// <summary>
        /// Handle events at the end of any import 
        /// </summary>
        public virtual IEnumerable<uSyncAction> ProcessPostImport(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
            => ProcessPostImport(actions, config);

        /// <summary>
        /// Handle events at the end of any import 
        /// </summary>
        public virtual IEnumerable<uSyncAction> ProcessPostImport(IEnumerable<uSyncAction> actions, HandlerSettings config)
        {
            if (actions == null || !actions.Any())
                return Enumerable.Empty<uSyncAction>();

            return CleanFolders(-1);
        }

        /// <summary>
        ///  will resave everything in a folder (and beneath)
        ///  we need to this when it's renamed
        /// </summary>
        protected IEnumerable<uSyncAction> UpdateFolder(int folderId, string folder, HandlerSettings config)
            => UpdateFolder(folderId, [folder], config);

        /// <summary>
        ///  will resave everything in a folder (and beneath)
        ///  we need to this when it's renamed
        /// </summary>
        protected IEnumerable<uSyncAction> UpdateFolder(int folderId, string[] folders, HandlerSettings config)
        {
            if (this.serializer is SyncContainerSerializerBase<TObject> containerSerializer)
            {
                containerSerializer.InitializeCache();
            }

            var actions = new List<uSyncAction>();
            var itemFolders = GetChildItems(folderId, this.itemContainerType);
            foreach (var item in itemFolders)
            {
                actions.AddRange(UpdateFolder(item.Id, folders, config));
            }

            var items = GetChildItems(folderId, this.itemObjectType);
            foreach (var item in items)
            {
                var obj = GetFromService(item.Id);
                if (obj != null)
                {
                    var attempts = Export(obj, folders, config);
                    foreach (var attempt in attempts.Where(x => x.Success))
                    {
                        // when its flat structure and use guidNames, we don't need to cleanup.
                        if (!(this.DefaultConfig.GuidNames && this.DefaultConfig.UseFlatStructure))
                        {
                            CleanUp(obj, attempt.FileName, folders.Last());
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
            // we are not handling saves, we assume a rename, is just that
            // if a rename does happen as part of a save then its only 
            // going to be part of an import, and that will rename the rest of the items
            // 
            // performance wise this is a big improvement, for very little/no impact

            // if (!ShouldProcessEvent()) return;
            // logger.LogDebug("Container(s) saved [{count}]", notification.SavedEntities.Count());
            // ProcessContainerChanges(notification.SavedEntities);
        }

        /// <summary>
        ///  handler renames of containers.
        /// </summary>
        public virtual void Handle(EntityContainerRenamedNotification notification)
        {
            if (!ShouldProcessEvent()) return;
            ProcessContainerChanges(notification.Entities);
        }

        private void ProcessContainerChanges(IEnumerable<EntityContainer> containers)
        {
            foreach (var folder in containers)
            {
                logger.LogDebug("Processing container change : {name} [{id}]", folder.Name, folder.Id);
                
                var targetFolders = rootFolders.Select(x => Path.Combine(x, DefaultFolder)).ToArray();

                if (folder.ContainedObjectType == this.itemObjectType.GetGuid())
                {
                    UpdateFolder(folder.Id, targetFolders, DefaultConfig);
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

        /// <summary>
        ///  Get merged items from a collection of folders. 
        /// </summary>
        protected override IReadOnlyList<OrderedNodeInfo> GetMergedItems(string[] folders)
        {
            var items = base.GetMergedItems(folders);
            
            CheckForDuplicates(items);

            var nodes = items.DistinctBy(x => x.Key).ToDictionary(k => k.Key);
            var renames = nodes.Where(x => x.Value.Node.IsEmptyItem()).Select(x => x.Value);
            var graph = new List<GraphEdge<Guid>>();

            foreach(var item in items)
            {
                graph.AddRange(GetCompositions(item.Node).Select(x => GraphEdge.Create(item.Key, x)));
            }

            var cleanGraph = graph.Where(x => x.Node == x.Edge).ToList();
            var sortedList = nodes.Keys.TopologicalSort(cleanGraph);

            if (sortedList == null)
                return items.OrderBy(x => x.Level).ToList();

            var results = new List<OrderedNodeInfo>();
            foreach (var key in sortedList)
            {
                if(nodes.TryGetValue(key, out OrderedNodeInfo value))
                    results.Add(value);
            }

            if (renames.Any())
            {
                results.RemoveAll(x => renames.Any(r => r.Key == x.Key));
				results.AddRange(renames);
			}

            return results;
        }

        private void CheckForDuplicates(IReadOnlyList<OrderedNodeInfo> items)
        {
            var duplicates = items
                .Where(x => x.Node?.IsEmptyItem() is false)
                .GroupBy(x => x.Key)
                .Where(x => x.Skip(1).Any()).ToArray();

            if (duplicates.Length > 0)
            {
                var dups = string.Join(" \n ", duplicates.SelectMany(x => x.Select(x => $"[{x.Path}]").ToArray()));
                logger.LogWarning("Duplicates: one or more items of the same type and key exist on disk [{duplicates}] the item to be imported cannot be guaranteed", dups);

                if (uSyncConfig.Settings.FailOnDuplicates)
                {
                    throw new InvalidOperationException($"Duplicate files detected. Check the disk. {dups}");
                }
            }
        }


        /// <summary>
        ///  get the Guid values of any compositions so they can be graphed
        /// </summary>
        public IEnumerable<Guid> GetGraphIds(XElement node)
        {
            return GetCompositions(node);
        }
    
        private IEnumerable<Guid> GetCompositions(XElement node)
        {
            var compositionNode = node.Element("Info")?.Element("Compositions");
            if (compositionNode == null) return Enumerable.Empty<Guid>();

            return GetKeys(compositionNode);
        }

        private IEnumerable<Guid> GetKeys(XElement node)
        {
            if (node != null)
            {
                foreach (var item in node.Elements())
                {
                    var key = item.Attribute("Key").ValueOrDefault(Guid.Empty);
                    if (key == Guid.Empty) continue;

                    yield return key;
                }
            }
        }
    }
}
