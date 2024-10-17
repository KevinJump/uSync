using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
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

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler to mange Media items in uSync
/// </summary>
[SyncHandler(uSyncConstants.Handlers.MediaHandler, "Media", "Media", uSyncConstants.Priorites.Media,
    Icon = "icon-picture", IsTwoPass = true, EntityType = UdiEntityType.Media)]
public class MediaHandler : ContentHandlerBase<IMedia>, ISyncHandler, ISyncCleanEntryHandler,
    INotificationAsyncHandler<SavedNotification<IMedia>>,
    INotificationAsyncHandler<DeletedNotification<IMedia>>,
    INotificationAsyncHandler<MovedNotification<IMedia>>,
    INotificationAsyncHandler<MovedToRecycleBinNotification<IMedia>>,
    INotificationAsyncHandler<SavingNotification<IMedia>>,
    INotificationAsyncHandler<DeletingNotification<IMedia>>,
    INotificationAsyncHandler<MovingNotification<IMedia>>,
    INotificationAsyncHandler<MovingToRecycleBinNotification<IMedia>>

{
    /// <inheritdoc/>
    public override string Group => uSyncConstants.Groups.Content;

    private readonly IMediaService mediaService;

    /// <inheritdoc/>
    public MediaHandler(
        ILogger<MediaHandler> logger,
        IEntityService entityService,
        IMediaService mediaService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        SyncFileService syncFileService,
        uSyncEventService mutexService,
        uSyncConfigService uSyncConfigService,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    {
        this.mediaService = mediaService;
    }

    /// <inheritdoc />
    protected override bool HasChildren(IMedia item)
        => mediaService.HasChildren(item.Id);

    /// <inheritdoc/>
    protected override async Task<IEnumerable<IEntity>> GetChildItemsAsync(IEntity? parent)
    {
        if (parent != null)
        {
            var items = new List<IMedia>();
            const int pageSize = 5000;
            var page = 0;
            var total = long.MaxValue;
            while (page * pageSize < total)
            {
                items.AddRange(mediaService.GetPagedChildren(parent.Id, page++, pageSize, out total));
            }
            return items;
        }
        else
        {
            return await Task.FromResult(mediaService.GetRootMedia());
        }
    }
}
