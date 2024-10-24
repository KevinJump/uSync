﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

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

    private readonly IContentService contentService;

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
        uSyncConfigService uSyncConfigService,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    {
        this.contentService = contentService;

        // make sure we get the default content serializer (not just the first one that loads)
        this.serializer = syncItemFactory.GetSerializer<IContent>("ContentSerializer")
            ?? throw new KeyNotFoundException("Cannot load content serializer");
    }

    /// <inheritdoc />
    protected override Task<bool> HasChildrenAsync(IContent item)
        => Task.FromResult(contentService.HasChildren(item.Id));

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
            if (parent is null) return contentService.GetRootContent();

            var items = new List<IContent>();
            const int pageSize = 5000;
            var page = 0;
            var total = long.MaxValue;
            while (page * pageSize < total)
            {
                items.AddRange(contentService.GetPagedChildren(parent.Id, page++, pageSize, out total));
            }
            return items;

        });
    }
}
