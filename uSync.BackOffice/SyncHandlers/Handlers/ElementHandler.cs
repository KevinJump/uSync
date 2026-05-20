using Microsoft.Extensions.Logging;

using Org.BouncyCastle.Security.Certificates;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

using static Umbraco.Cms.Core.Constants;
using static Umbraco.Cms.Core.Constants.HttpContext;

namespace uSync.BackOffice.SyncHandlers.Handlers;

[SyncHandler(
    alias: uSyncConstants.Handlers.ElementHandler,
    name: "Library", folder: "Element",
    priority: uSyncConstants.Priorites.Elements,
    Icon = "icon-books", IsTwoPass = true, EntityType = UdiEntityType.Element)]
public class ElementHandler : PublishableContentHandlerBase<IElement>, ISyncHandler,
    INotificationAsyncHandler<SavedNotification<IElement>>,
    INotificationAsyncHandler<DeletedNotification<IElement>>,
    INotificationAsyncHandler<ElementPublishedNotification>,
    INotificationAsyncHandler<ElementUnpublishedNotification>,
    INotificationAsyncHandler<MovedNotification<IElement>>,
    INotificationAsyncHandler<MovedToRecycleBinNotification<IElement>>,
    INotificationAsyncHandler<SavingNotification<IElement>>,
    INotificationAsyncHandler<DeletingNotification<IElement>>,
    INotificationAsyncHandler<MovingNotification<IElement>>,
    INotificationAsyncHandler<MovingToRecycleBinNotification<IElement>>
{
    private readonly IElementService _elementService;
    private readonly IElementContainerService _containerService;

    private readonly IList<ISyncTracker<EntityContainer>> _treeTrackers;
    private readonly ISyncEntityContainerSerializer<EntityContainer> _containerSerializer;

    public override string Group => uSyncConstants.Groups.Content;

    public ElementHandler(
        ILogger<ContentHandlerBase<IElement>> logger,
        IEntityService entityService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfigService,
        Core.ISyncItemFactory syncItemFactory,
        IElementService elementService,
        IElementContainerService containerService)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    {
        _elementService = elementService;

        _treeTrackers = [.. this.itemFactory.GetTrackers<EntityContainer>()];

        _containerSerializer = this.itemFactory
            .GetContainerSerializers<EntityContainer>(UmbracoObjectTypes.Element)
            .FirstOrDefault()
            ?? throw new Exception($"No container serializer found for {UmbracoObjectTypes.Element}");

        ItemObjectType = UmbracoObjectTypes.Element;
        ItemContainerType = UmbracoObjectTypes.ElementContainer;
        _containerService = containerService;
    }

    /// <inheritdoc />
    protected override Task<bool> HasChildrenAsync(IElement item)
        => Task.FromResult(false);

    protected override IEnumerable<IEntity> GetRootItems()
    {
        var rootElements = entityService.GetRootEntities(UmbracoObjectTypes.Element);
        return rootElements;
    }

    private async Task<EntityContainer?> GetContainer(Guid key)
        => await _containerService.GetAsync(key);

    public override async Task<IEnumerable<uSyncAction>> ExportContainer(IEntity item, string[] folders, HandlerSettings config)
    {
        if (item is null)
            return [uSyncAction.Fail(nameof(item), this.handlerType, this.ItemType, ChangeType.Fail, "Item not set",
                new ArgumentNullException(nameof(item)))];

        // nothing to export ? 
        var container = await GetContainer(item.Key);
        if (container == null) return [];

        if (await _mutexService.FireItemStartingEventAsync(new CancelableuSyncItemNotification<EntityContainer>(container)))
        {
            return [uSyncActionHelper<IEntity>
                .ReportAction(ChangeType.NoChange, GetContainerName(container), string.Empty, string.Empty, GetContainerKey(container), this.Alias,
                                "Change stopped by delegate event")];
        }

        var targetFolder = folders.Last();

        var filename = (await GetContainerPathAsync(targetFolder, container, config.GuidNames, config.UseFlatStructure))
            .ToAppSafeFileName();

        // 
        if (IsLockedAtRoot(folders, filename.Substring(targetFolder.Length + 1)))
        {
            // if we have lock roots on, then this item will not export 
            // because exporting would mean the root was no longer used.
            return [uSyncAction.SetAction(true, syncFileService.GetSiteRelativePath(filename),
                type: typeof(IElement).ToString(),
                change: ChangeType.NoChange,
                message: "Not exported (would overwrite root value)",
                filename: filename)];
        }


        var attempt = await Export_DoContainerExport(container, filename, folders, config);

        if (attempt.Change > ChangeType.NoChange)
            await _mutexService.FireItemCompletedEventAsync(new uSyncExportedItemNotification(attempt.Item, ChangeType.Export));

        return [uSyncActionHelper<XElement>.SetAction(attempt, syncFileService.GetSiteRelativePath(filename), GetContainerKey(container), this.Alias)];
    }

    private async Task<SyncAttempt<XElement>> Export_DoContainerExport(EntityContainer item, string filename, string[] folders, HandlerSettings config)
    {
        var attempt = await SerializeContainerAsync(item, new SyncSerializerOptions(config.Settings));
        if (attempt.Success is false || attempt.Item is null) return attempt;

        if (await ShouldExportAsync(attempt.Item, config) is false)
            return SyncAttempt<XElement>.Succeed(Path.GetFileName(filename), ChangeType.NoChange, "Not Exported (Based on configuration)");

        var files = await Task.WhenAll(folders
            .Select(async x => await GetContainerPathAsync(x, item, config.GuidNames, config.UseFlatStructure))
            .ToArray());

        var nodes = await syncFileService.GetAllNodesAsync(files[..^1]);
        if (nodes.Count > 0)
        {
            nodes.Add(attempt.Item);
            var differences = syncFileService.GetDifferences(nodes, trackers.FirstOrDefault());
            if (differences is not null && differences.HasElements)
            {
                if (config.FullFileOnDifference)
                {
                    await syncFileService.SaveXElementAsync(attempt.Item, filename);
                }
                else
                {
                    await syncFileService.SaveXElementAsync(differences, filename);
                }
            }
            else
            {

                if (syncFileService.FileExists(filename))
                {
                    // we don't delete them - because in deployments they might then hang around
                    // we mark them as reverted and then they don't get processed.
                    var emptyNode = XElementExtensions.MakeEmpty(attempt.Item.GetKey(), SyncActionType.None, "Reverted to root");
                    await syncFileService.SaveXElementAsync(emptyNode, filename);
                }
            }
        }
        else
        {
            await syncFileService.SaveXElementAsync(attempt.Item, filename);
        }

        if (config.CreateClean)
            await CreateCleanFileAsync(GetContainerKey(item), filename);

        return attempt;
    }

    protected async Task<SyncAttempt<XElement>> SerializeContainerAsync(EntityContainer item, SyncSerializerOptions options)
        => await _containerSerializer.SerializeAsync(item, options);

    private async Task<string> GetContainerPathAsync(string folder, EntityContainer item, bool GuidNames, bool isFlat)
    {
        if (isFlat && GuidNames) return Path.Combine(folder, $"{GetContainerKey(item)}.{this.uSyncConfig.Settings.DefaultExtension}");
        var path = Path.Combine(folder, $"{GetContainerEntityPath(item, GuidNames, isFlat)}.{this.uSyncConfig.Settings.DefaultExtension}");

        // if this is flat but not using GUID filenames, then we check for clashes.
        if (isFlat && !GuidNames) return await CheckAndFixContainerFileClashAsync(path, item);
        return path;
    }

    private Guid GetContainerKey(EntityContainer item) => item.Key;
    private string GetContainerName(EntityContainer item) => item.Name ?? item.Id.ToString();

    private string GetContainerEntityPath(EntityContainer item, bool GuidNames, bool isFlat)
        => GuidNames ? GetContainerKey(item).ToString() : GetContainerName(item).ToAppSafeFileName();

    virtual protected async Task<string> CheckAndFixContainerFileClashAsync(string path, EntityContainer item)
    {
        if (syncFileService.FileExists(path))
        {
            var node = await syncFileService.LoadXElementAsync(path);

            if (node == null) return path;
            if (GetContainerKey(item) == node.GetKey()) return path;
            if (GetXmlMatchString(node).Equals(GetContainerItemMatchString(item), StringComparison.InvariantCultureIgnoreCase)) return path;

            // get here we have a clash, we should append something
            var append = GetContainerKey(item).ToShortKeyString(8); // (this is the shortened GUID like media folders do)
            return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                Path.GetFileNameWithoutExtension(path) + "_" + append + Path.GetExtension(path));
        }

        return path;
    }

    private string GetContainerItemMatchString(EntityContainer item)
    {
        var itemPath = item.Level.ToString();
        if (item.Trashed)
            itemPath = _containerSerializer.GetEntityContainerPath(item);

        return $"{item.Name}_{itemPath}";
    }

    /// <summary>
    ///  Handle the publish events for content
    /// </summary>
    /// <remarks>
    ///  some publication events do not fire the save notification, so we need to handle those here.
    /// </remarks>
    public async Task HandleAsync(ElementPublishedNotification notification, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent()) return;
        if (notification.State.TryGetValue(uSync.EventPausedKey, out var paused) && paused is true)
            return;

        var handlerFolders = GetDefaultHandlerFolders();

        foreach (var item in notification.PublishedEntities)
        {
            await ProcessItem(notification, item, handlerFolders);
        }
    }

    /// <summary>
    ///  un-publish content items, this is called when content is unpublished (not deleted)
    /// </summary>
    /// <remarks>
    ///  un-publish does not fire the save notification, so we need to handle those here.
    /// </remarks>
    public async Task HandleAsync(ElementUnpublishedNotification notification, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent()) return;
        if (notification.State.TryGetValue(uSync.EventPausedKey, out var paused) && paused is true)
            return;

        var handlerFolders = GetDefaultHandlerFolders();

        foreach (var item in notification.UnpublishedEntities)
        {
            await ProcessItem(notification, item, handlerFolders);
        }
    }

    protected override async Task<uSyncAction> DeserializeItemToAction(XElement node, string filename, SyncSerializerOptions serializerOptions)
    {
        if (node.Name.LocalName == global::uSync.Core.uSyncConstants.Serialization.ElementContainer)
        {
            var containerAttempt = await _containerSerializer.DeserializeAsync(node, serializerOptions);
            var containerAction = uSyncActionHelper<EntityContainer>.SetAction(containerAttempt, GetNameFromFileOrNode(filename, node), node.GetKey(), this.Alias, IsTwoPass);

            if (containerAttempt.Item != null) containerAction.Item = containerAttempt.Item;
            if (containerAttempt.Details != null && containerAttempt.Details.Any()) containerAction.Details = containerAttempt.Details;

            return containerAction;
        }

        // get the item.
        var attempt = await DeserializeItemAsync(node, serializerOptions);
        var action = uSyncActionHelper<IElement>.SetAction(attempt, GetNameFromFileOrNode(filename, node), node.GetKey(), this.Alias, IsTwoPass);

        // add item if we have it.
        if (attempt.Item != null) action.Item = attempt.Item;

        // add details if we have them
        if (attempt.Details != null && attempt.Details.Any()) action.Details = attempt.Details;
        return action;
    }
}
