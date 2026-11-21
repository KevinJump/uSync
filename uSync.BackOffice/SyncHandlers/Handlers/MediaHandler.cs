using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System.Linq;
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
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Core.Serialization;

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

    private readonly IMediaService _mediaService;

    /// <inheritdoc/>
    public MediaHandler(
        ILogger<MediaHandler> logger,
        IEntityService entityService,
        IMediaService mediaService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfigService,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    {
        _mediaService = mediaService;
    }

    /// <inheritdoc />
    protected override Task<bool> HasChildrenAsync(IMedia item)
        => Task.FromResult(_mediaService.HasChildren(item.Id));

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
                items.AddRange(_mediaService.GetPagedChildren(parent.Id, page++, pageSize, out total));
            }

            // one (light) entity query tells us which children have no children of their own,
            // so the export doesn't have to query for the children of every leaf item.
            if (items.Count > 0) RememberLeaves(entityService.GetChildren(parent.Id));
            return items;
        }
        else
        {
            var rootItems = _mediaService.GetRootMedia().ToList();
            if (rootItems.Count > 0) RememberLeaves(entityService.GetChildren(Umbraco.Cms.Core.Constants.System.Root));
            return await Task.FromResult(rootItems);
        }
    }

    /// <inheritdoc/>
    public override Task<SyncItemPreview?> GetPreviewAsync(XElement node, HandlerSettings settings, SyncPreviewOptions? options = null)
        => BuildPreviewAsync(node, settings, SerializerFlags.None, options);
}
