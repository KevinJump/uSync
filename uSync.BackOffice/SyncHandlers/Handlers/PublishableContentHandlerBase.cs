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
using uSync.Core;
using uSync.Core.Extensions;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Base handler for content types that can be published (documents, elements, etc).
/// </summary>
public abstract class PublishableContentHandlerBase<TObject>
    : ContentHandlerBase<TObject>
    where TObject : class, IPublishableContentBase
{
    /// <summary>
    ///  The Umbraco object type used for containers (folders) for this handler.
    /// </summary>
    protected UmbracoObjectTypes ContainerType => UmbracoObjectTypes.Document;

    /// <summary>
    ///  Constructor
    /// </summary>
    protected PublishableContentHandlerBase(
        ILogger<ContentHandlerBase<TObject>> logger,
        IEntityService entityService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfigService,
        ISyncItemFactory syncItemFactory) 
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    { }

    /// <summary>
    ///  Get the root level items for this handler (items with no parent).
    /// </summary>
    protected abstract IEnumerable<IEntity> GetRootItems();

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
            if (parent is null) return GetRootItems();

            var items = new List<IEntitySlim>();
            const int pageSize = 5000;
            var page = 0;
            var total = long.MaxValue;
            while (page * pageSize < total)
            {
                items.AddRange(entityService.GetPagedChildren(parent.Id, ItemObjectType, page++, pageSize, out total));
            }
            return items;

        });
    }


    /// <summary>
    ///  Export a single item in response to a notification, cleaning up any orphaned files afterwards.
    /// </summary>
    protected async Task ProcessItem(EnumerableObjectNotification<TObject> notification, TObject item, string[] handlerFolders)
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
