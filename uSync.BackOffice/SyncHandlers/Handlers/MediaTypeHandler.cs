using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler to mange Media Types in uSync
/// </summary>
[SyncHandler(uSyncConstants.Handlers.MediaTypeHandler, "Media Types", "MediaTypes", uSyncConstants.Priorites.MediaTypes,
    IsTwoPass = true, Icon = "icon-thumbnails", EntityType = UdiEntityType.MediaType)]
public class MediaTypeHandler : ContentTypeBaseHandler<IMediaType>, ISyncHandler, ISyncPostImportHandler, ISyncGraphableHandler,
    INotificationHandler<SavedNotification<IMediaType>>,
    INotificationHandler<DeletedNotification<IMediaType>>,
    INotificationHandler<MovedNotification<IMediaType>>,
    INotificationHandler<EntityContainerSavedNotification>,
    INotificationHandler<EntityContainerRenamedNotification>,
    INotificationHandler<SavingNotification<IMediaType>>,
    INotificationHandler<DeletingNotification<IMediaType>>,
    INotificationHandler<MovingNotification<IMediaType>>
{
    private readonly IMediaTypeContainerService _mediaTypeContainerService;

    /// <inheritdoc/>
    public MediaTypeHandler(
        ILogger<MediaTypeHandler> logger,
        IEntityService entityService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        SyncFileService syncFileService,
        uSyncEventService mutexService,
        uSyncConfigService uSyncConfig,
        ISyncItemFactory syncItemFactory,
        IMediaTypeContainerService mediaTypeContainerService)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
    {
        _mediaTypeContainerService = mediaTypeContainerService;
    }

    /// <inheritdoc/>
    protected override string GetEntityTreeName(IUmbracoEntity item, bool useGuid)
    {
        if (useGuid) return item.Key.ToString();

        if (item is IMediaType mediaType)
        {
            return mediaType.Alias.ToSafeFileName(shortStringHelper);
        }

        return item.Name?.ToSafeFileName(shortStringHelper) ?? item.Key.ToString();
    }

    protected override async Task DeleteFolderAsync(Guid key)
        => await _mediaTypeContainerService.DeleteAsync(key, Constants.Security.SuperUserKey);

    protected override async Task<IEntity?> GetContainerAsync(Guid key)
        => await _mediaTypeContainerService.GetAsync(key);

}
