using Microsoft.Extensions.Logging;

using System;
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
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler to manage Dictionary items via uSync 
/// </summary>
[SyncHandler(uSyncConstants.Handlers.DictionaryHandler, "Dictionary", "Dictionary", uSyncConstants.Priorites.DictionaryItems
    , Icon = "icon-book-alt", EntityType = UdiEntityType.DictionaryItem)]
public class DictionaryHandler : SyncHandlerLevelBase<IDictionaryItem>, ISyncHandler,
    INotificationAsyncHandler<SavedNotification<IDictionaryItem>>,
    INotificationAsyncHandler<DeletedNotification<IDictionaryItem>>,
    INotificationAsyncHandler<SavingNotification<IDictionaryItem>>,
    INotificationAsyncHandler<DeletingNotification<IDictionaryItem>>
{
    /// <summary>
    ///  Dictionary items belong to the content group by default
    /// </summary>
    public override string Group => uSyncConstants.Groups.Content;

    private readonly IDictionaryItemService _dictionaryItemService;

    /// <inheritdoc/>
    public DictionaryHandler(
        ILogger<DictionaryHandler> logger,
        IEntityService entityService,
        IDictionaryItemService dictionaryItemService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfigService,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    {
        _dictionaryItemService = dictionaryItemService;
    }

    /// <inheritdoc/>
    protected override async Task<IEnumerable<IEntity>> GetFoldersAsync(Guid key)
        => await GetChildItemsAsync(key);

    /// <inheritdoc/>
    protected override async Task<IEnumerable<IEntity>> GetChildItemsAsync(Guid key)
    {
        if (key == Guid.Empty)
        {
            return (await _dictionaryItemService.GetAtRootAsync())
                .Where(x => x is IEntity)
                .Select(x => x as IEntity);
        }
        else
        {
            var item = await _dictionaryItemService.GetAsync(key);
            if (item != null)
                return await _dictionaryItemService.GetChildrenAsync(item.Key);
        }

        return [];
    }

    /// <inheritdoc/>
    protected override string GetItemName(IDictionaryItem item)
        => item.ItemKey;

    /// <inheritdoc/>
    protected override string GetItemPath(IDictionaryItem item, bool useGuid, bool isFlat)
        => item.ItemKey.ToSafeFileName(shortStringHelper);
}

  
