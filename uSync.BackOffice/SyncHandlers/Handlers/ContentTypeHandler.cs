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
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler to mange content types in uSync
/// </summary>
[SyncHandler(uSyncConstants.Handlers.ContentTypeHandler, "DocTypes", "ContentTypes", uSyncConstants.Priorites.ContentTypes,
        IsTwoPass = true, Icon = "icon-item-arrangement", EntityType = UdiEntityType.DocumentType)]
public class ContentTypeHandler : ContentTypeBaseHandler<IContentType>, ISyncHandler, ISyncPostImportHandler, ISyncGraphableHandler,
    INotificationAsyncHandler<SavedNotification<IContentType>>,
    INotificationAsyncHandler<DeletedNotification<IContentType>>,
    INotificationAsyncHandler<MovedNotification<IContentType>>,
    INotificationAsyncHandler<EntityContainerSavedNotification>,
    INotificationAsyncHandler<EntityContainerRenamedNotification>,
    INotificationAsyncHandler<SavingNotification<IContentType>>,
    INotificationAsyncHandler<MovingNotification<IContentType>>,
    INotificationAsyncHandler<DeletingNotification<IContentType>>
{
    private readonly IContentTypeContainerService _contentTypeContainerService;

    /// <summary>
    ///  Constructor - loaded via DI
    /// </summary>
    public ContentTypeHandler(
        ILogger<ContentTypeHandler> logger,
        IEntityService entityService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        SyncFileService syncFileService,
        uSyncEventService mutexService,
        uSyncConfigService uSyncConfig,
        ISyncItemFactory syncItemFactory,
        IContentTypeContainerService contentTypeContainerService)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
    {
        _contentTypeContainerService = contentTypeContainerService;
    }

    /// <summary>
    ///  Get the entity name we are going to use when constructing a generic path for an item
    /// </summary>
    protected override string GetEntityTreeName(IUmbracoEntity item, bool useGuid)
    {
        if (useGuid) return item.Key.ToString();

        if (item is IContentTypeBase baseItem)
        {
            return baseItem.Alias.ToSafeFileName(shortStringHelper);
        }

        return item.Name?.ToSafeFileName(shortStringHelper) ?? item.Key.ToString();
    }

    protected override async Task<IEntity?> GetContainerAsync(Guid key)
        => await _contentTypeContainerService.GetAsync(key);

    protected override async Task DeleteFolderAsync(Guid key)
        => await _contentTypeContainerService.DeleteAsync(key, Constants.Security.SuperUserKey);
}
