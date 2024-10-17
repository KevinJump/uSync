using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

namespace uSync.BackOffice.SyncHandlers;

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
public abstract class SyncHandlerContainerBase<TObject>
    : SyncHandlerTreeBase<TObject>
    where TObject : ITreeEntity
{

    /// <inheritdoc/>
    protected SyncHandlerContainerBase(
        ILogger<SyncHandlerContainerBase<TObject>> logger,
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
    [Obsolete("use CleanFoldersAsync will be removed in v16")]
    protected IEnumerable<uSyncAction> CleanFolders(int parent)
        => [];

    protected async Task<IEnumerable<uSyncAction>> CleanFoldersAsync(Guid parentKey)
    {
        var actions = new List<uSyncAction>();
        var folders = await GetChildItemsAsync(parentKey, this.ItemContainerType);
        foreach (var folder in folders)
        {
            if (folder is null) continue;

            logger.LogDebug("Checking Container: {folder} for any childItems [{type}]", folder.Id, folder.GetType()?.Name ?? "Unknown");
            actions.AddRange(await CleanFoldersAsync(folder.Key));

            if (!await HasChildrenAsync(folder))
            {
                // get the name (from the slim)
                var name = folder.Id.ToString() ?? string.Empty;
                if (folder is IEntitySlim slim)
                {
                    // if this item isn't an container type, don't delete. 
                    if (ObjectTypes.GetUmbracoObjectType(slim.NodeObjectType) != this.ItemContainerType) continue;

                    name = slim.Name ?? name;
                    logger.LogDebug("Folder has no children {name} {type}", name, slim.NodeObjectType);
                }

                actions.Add(uSyncAction.SetAction(true, name, typeof(EntityContainer).Name, ChangeType.Delete, "Empty Container"));
                await DeleteFolderAsync(folder.Key);
            }
        }

        return actions;
    }

    private static bool IsContainer(Guid guid)
        => guid == Constants.ObjectTypes.DataTypeContainer
        || guid == Constants.ObjectTypes.MediaTypeContainer
        || guid == Constants.ObjectTypes.DocumentTypeContainer;

    /// <summary>
    /// delete a container
    /// </summary>
    [Obsolete("Delete by key - will be removed in v16")]
    virtual protected void DeleteFolder(int id) { }

    abstract protected Task DeleteFolderAsync(Guid key);
    /// <summary>
    /// Handle events at the end of any import 
    /// </summary>
    [Obsolete("Use ProcessPostImportAsync will be removed in v16")]
    public virtual IEnumerable<uSyncAction> ProcessPostImport(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
        => ProcessPostImport(actions, config);

    /// <summary>
    /// Handle events at the end of any import 
    /// </summary>
    [Obsolete("Use ProcessPostImportAsync will be removed in v16")]
    public virtual IEnumerable<uSyncAction> ProcessPostImport(IEnumerable<uSyncAction> actions, HandlerSettings config)
       => ProcessPostImportAsync(actions, config).Result;

    public virtual async Task<IEnumerable<uSyncAction>> ProcessPostImportAsync(IEnumerable<uSyncAction> actions, HandlerSettings config)
    {
        if (actions == null || !actions.Any()) return [];

        var results = new List<uSyncAction>();
        var options = new uSyncImportOptions { Flags = SerializerFlags.LastPass };

        // we only do deletes here. 
        foreach (var action in actions.Where(x => x.Change == ChangeType.Hidden))
        {
            if (action.FileName is null) continue;
            results.AddRange(await ImportAsync(action.FileName, config, options));
        }

        results.AddRange(await CleanFoldersAsync(Guid.Empty));

        return results;
    }

    /// <summary>
    ///  will resave everything in a folder (and beneath)
    ///  we need to this when it's renamed
    /// </summary>
    [Obsolete("Use UpdateFolderAsync (and pass a guid")]
    protected IEnumerable<uSyncAction> UpdateFolder(int folderId, string folder, HandlerSettings config)
        => UpdateFolder(folderId, [folder], config);

    /// <summary>
    ///  will resave everything in a folder (and beneath)
    ///  we need to this when it's renamed
    /// </summary>
    [Obsolete("Use UpdateFolderAsync (and pass a guid")]
    protected IEnumerable<uSyncAction> UpdateFolder(int folderId, string[] folders, HandlerSettings config)
        => [];
    
    protected async Task<IEnumerable<uSyncAction>> UpdateFolderAsync(Guid folderKey, string[] folders, HandlerSettings config)
    {
        if (this.serializer is SyncContainerSerializerBase<TObject> containerSerializer)
        {
            containerSerializer.InitializeCache();
        }

        var actions = new List<uSyncAction>();
        var itemFolders = await GetChildItemsAsync(folderKey, this.ItemContainerType);
        foreach (var item in itemFolders)
        {
            actions.AddRange(await UpdateFolderAsync(item.Key, folders, config));
        }

        var items = await GetChildItemsAsync(folderKey, this.ItemObjectType);
        foreach (var item in items)
        {
            var obj = await GetFromServiceAsync(item.Key);
            if (obj != null)
            {
                var attempts = await ExportAsync(obj, folders, config);
                foreach (var attempt in attempts.Where(x => x.Success))
                {
                    // when its flat structure and use guidNames, we don't need to cleanup.
                    if ((this.DefaultConfig.GuidNames && this.DefaultConfig.UseFlatStructure) is false)
                    {
                        if (attempt.FileName is not null)
                        {
                            await CleanUpAsync(obj, attempt.FileName, folders.Last());
                        }
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
    public virtual Task HandleAsync(EntityContainerSavedNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
        // we are not handling saves, we assume a rename, is just that
        // if a rename does happen as part of a save then its only 
        // going to be part of an import, and that will rename the rest of the items
        // 
        // performance wise this is a big improvement, for very little/no impact

        // if (!ShouldProcessEvent()) return;
        // logger.LogDebug("Container(s) saved [{count}]", notification.SavedEntities.Count());
        // ProcessContainerChanges(notification.SavedEntities);
    }

   
    public virtual async Task HandleAsync(EntityContainerRenamedNotification notification, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent()) return;
        await ProcessContainerChangesAsync(notification.Entities);
    }

    private async Task ProcessContainerChangesAsync(IEnumerable<EntityContainer> containers)
    {
        foreach (var folder in containers)
        {
            logger.LogDebug("Processing container change : {name} [{id}]", folder.Name, folder.Id);

            var targetFolders = RootFolders.Select(x => Path.Combine(x, DefaultFolder)).ToArray();

            if (folder.ContainedObjectType == this.ItemObjectType.GetGuid())
            {
                await UpdateFolderAsync(folder.Key, targetFolders, DefaultConfig);
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

        foreach (var item in items)
        {
            graph.AddRange(SyncHandlerContainerBase<TObject>.GetCompositions(item.Node).Select(x => GraphEdge.Create(item.Key, x)));
        }

        var cleanGraph = graph.Where(x => x.Node == x.Edge).ToList();
        var sortedList = nodes.Keys.TopologicalSort(cleanGraph);

        if (sortedList is null)
            return [.. items.OrderBy(x => x.Level)];

        var results = new List<OrderedNodeInfo>();
        foreach (var key in sortedList)
        {
            if (nodes.TryGetValue(key, out OrderedNodeInfo? value) && value is not null)
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
        return SyncHandlerContainerBase<TObject>.GetCompositions(node);
    }

    private static IEnumerable<Guid> GetCompositions(XElement node)
    {
        var compositionNode = node.Element("Info")?.Element("Compositions");
        if (compositionNode == null) return [];

        return SyncHandlerContainerBase<TObject>.GetKeys(compositionNode);
    }

    private static IEnumerable<Guid> GetKeys(XElement node)
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
