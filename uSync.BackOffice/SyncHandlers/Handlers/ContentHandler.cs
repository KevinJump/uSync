using System.Collections.Generic;

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
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler to manage content items in uSync
/// </summary>
[SyncHandler(uSyncConstants.Handlers.ContentHandler, "Content", "Content", uSyncConstants.Priorites.Content
    , Icon = "icon-document", IsTwoPass = true, EntityType = UdiEntityType.Document)]
public class ContentHandler : ContentHandlerBase<IContent, IContentService>, ISyncHandler,
    INotificationHandler<SavedNotification<IContent>>,
    INotificationHandler<DeletedNotification<IContent>>,
    INotificationHandler<MovedNotification<IContent>>,
    INotificationHandler<MovedToRecycleBinNotification<IContent>>,
    INotificationHandler<SavingNotification<IContent>>,
    INotificationHandler<DeletingNotification<IContent>>,
    INotificationHandler<MovingNotification<IContent>>,
    INotificationHandler<MovingToRecycleBinNotification<IContent>>

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
        SyncFileService syncFileService,
        uSyncEventService mutexService,
        uSyncConfigService uSyncConfigService,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    {
        this.contentService = contentService;

        // make sure we get the default content serializer (not just the first one that loads)
        var s = syncItemFactory.GetSerializer<IContent>("ContentSerializer");
        if (s is null)
            throw new KeyNotFoundException("Cannot load content serializer");
        this.serializer = s;
    }

    /// <inheritdoc />
    protected override bool HasChildren(IContent item)
        => contentService.HasChildren(item.Id);

    /// <summary>
    ///  Get child items 
    /// </summary>
    /// <remarks>
    ///  The core method works for all services, (using entities) - but if we look up
    ///  the actual type for content and media, we save ourselves an extra lookup later on
    ///  and this speeds up the itteration by quite a bit (onle less db trip per item).
    /// </remarks>
    protected override IEnumerable<IEntity> GetChildItems(IEntity? parent)
    {
        if (parent != null)
        {
            var items = new List<IContent>();
            const int pageSize = 5000;
            var page = 0;
            var total = long.MaxValue;
            while (page * pageSize < total)
            {
                items.AddRange(contentService.GetPagedChildren(parent.Id, page++, pageSize, out total));
            }
            return items;
        }
        else
        {
            return contentService.GetRootContent();
        }
    }
}
