using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

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
using uSync.Core.Serialization;

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
        uSyncConfigService uSyncConfigService,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    {
        _dictionaryItemService = dictionaryItemService;
    }

    /// <inheritdoc/>
    public override async Task<IEnumerable<uSyncAction>> ImportAsync(string file, HandlerSettings config, uSyncImportOptions options)
    { 
        if (IsOneWay(config))
        {
            // only sync dictionary items if they are new
            // so if it already exists we don't do the sync

            //
            // <Handler Alias="dictionaryHandler" Enabled="true">
            //    <Add Key="OneWay" Value="true" />
            // </Handler>
            //
            var item = await GetExistingItemAsync(file);
            if (item != null)
            {
                return uSyncAction.SetAction(true, item.ItemKey, change: ChangeType.NoChange).AsEnumerableOfOne();
            }
        }

        return await base.ImportAsync(file, config, options);

    }

    private async Task<IDictionaryItem?> GetExistingItemAsync(string filePath)
    {
        syncFileService.EnsureFileExists(filePath);

        var node = await syncFileService.LoadXElementAsync(filePath);
        return await serializer.FindItemAsync(node);
    }

    /// <inheritdoc/>
    protected override async Task<IEnumerable<IEntity>> GetFoldersAsync(Guid key)
        => await GetChildItemsAsync(key);

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

        return Enumerable.Empty<IEntity>();
    }

    /// <inheritdoc/>
    protected override string GetItemName(IDictionaryItem item)
        => item.ItemKey;

    /// <inheritdoc/>
    protected override string GetItemPath(IDictionaryItem item, bool useGuid, bool isFlat)
        => item.ItemKey.ToSafeFileName(shortStringHelper);

    /// <inheritdoc/>
    public override async Task<IEnumerable<uSyncAction>> ReportElementAsync(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options)
    {
        if (IsOneWay(settings))
        {
            // we check if there is no change, we don't report it.
            // if we find it then there is no change. 
            var item = await GetExistingItemAsync(filename);
            if (item != null)
            {
                return uSyncActionHelper<IDictionaryItem>
                    .ReportAction(ChangeType.NoChange, item.ItemKey, node.GetPath(), syncFileService.GetSiteRelativePath(filename), item.Key, this.Alias, "Existing Item will not be overwritten")
                    .AsEnumerableOfOne<uSyncAction>();
            }
        }

        return await base.ReportElementAsync(node, filename, settings, options);
    }

    private bool IsOneWay(HandlerSettings? config)
        => config?.GetSetting("OneWay", false) == true;
}
