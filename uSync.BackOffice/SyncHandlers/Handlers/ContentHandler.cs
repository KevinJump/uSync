using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
using uSync.Core.Extensions;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler to manage content items in uSync
/// </summary>
[SyncHandler(uSyncConstants.Handlers.ContentHandler, "Content", "Content", uSyncConstants.Priorites.Content
    , Icon = "icon-document", IsTwoPass = true, EntityType = UdiEntityType.Document)]
public class ContentHandler : ContentHandlerBase<IContent>, ISyncHandler,

    INotificationAsyncHandler<SavedNotification<IContent>>,
    INotificationAsyncHandler<DeletedNotification<IContent>>,
    INotificationAsyncHandler<ContentPublishedNotification>,
    INotificationAsyncHandler<ContentUnpublishedNotification>,
    INotificationAsyncHandler<MovedNotification<IContent>>,
    INotificationAsyncHandler<MovedToRecycleBinNotification<IContent>>,
    INotificationAsyncHandler<SavingNotification<IContent>>,
    INotificationAsyncHandler<DeletingNotification<IContent>>,
    INotificationAsyncHandler<MovingNotification<IContent>>,
    INotificationAsyncHandler<MovingToRecycleBinNotification<IContent>>

{
    /// <summary>
    ///  the default group for which events matter (content group)
    /// </summary>
    public override string Group => uSyncConstants.Groups.Content;

    private readonly IContentService _contentService;

    /// <summary>
    /// Constructor, called via DI
    /// </summary>
    public ContentHandler(
        ILogger<ContentHandler> logger,
        IEntityService entityService,
        IContentService contentService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfigService,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    {
        _contentService = contentService;

        // make sure we get the default content serializer (not just the first one that loads)
        this.serializer = syncItemFactory.GetSerializer<IContent>("ContentSerializer")
            ?? throw new KeyNotFoundException("Cannot load content serializer");
    }

    /// <inheritdoc />
    protected override Task<bool> HasChildrenAsync(IContent item)
        => Task.FromResult(_contentService.HasChildren(item.Id));

    /// <summary>
    ///  Get child items 
    /// </summary>
    /// <remarks>
    ///  The core method works for all services, (using entities) - but if we look up
    ///  the actual type for content and media, we save ourselves an extra lookup later on
    ///  and this speeds up the itteration by quite a bit (onle less db trip per item).
    /// </remarks>
    protected override Task<IEnumerable<IEntity>> GetChildItemsAsync(IEntity? parent)
    {
        return uSyncTaskHelper.FromResultOf<IEnumerable<IEntity>>(() =>
        {
            if (parent is null) return _contentService.GetRootContent();

            var items = new List<IContent>();
            const int pageSize = 5000;
            var page = 0;
            var total = long.MaxValue;
            while (page * pageSize < total)
            {
                items.AddRange(_contentService.GetPagedChildren(parent.Id, page++, pageSize, out total));
            }
            return items;

        });
    }

    public async Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
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

    public async Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent()) return;
        if (notification.State.TryGetValue(uSync.EventPausedKey, out var paused) && paused is true)
            return;

        var handlerFolders = GetDefaultHandlerFolders();

        foreach(var item in notification.UnpublishedEntities)
        {
            await ProcessItem(notification, item, handlerFolders);
        }
    }

    private async Task ProcessItem(EnumerableObjectNotification<IContent> notification, IContent item, string[] handlerFolders)
    {
        try
        {
            var attempts = await ExportAsync(item, handlerFolders, DefaultConfig);
            foreach (var attempt in attempts.Where(x => x.Success))
            {
                if (attempt.FileName is null) continue;
                await this.CleanUpAsync(item, attempt.FileName, handlerFolders.Last());
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create uSync export file");
            notification.Messages.Add(new EventMessage("uSync", $"Failed to create export file : {ex.Message}", EventMessageType.Warning));
        }
    }
}
