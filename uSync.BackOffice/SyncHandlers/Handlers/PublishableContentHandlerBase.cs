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
    ///  Handle the Umbraco saved notification for publishable items.
    /// </summary>
    /// <remarks>
    ///  routed through <see cref="ProcessItem(EnumerableObjectNotification{TObject}, TObject, string[])"/>
    ///  rather than using the base implementation, so the export is de-duplicated against the
    ///  published/unpublished notifications raised by the same operation.
    /// </remarks>
    public override async Task HandleAsync(SavedNotification<TObject> notification, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent()) return;
        if (notification.State.TryGetValue(uSync.EventPausedKey, out var paused) && paused is true)
            return;

        var handlerFolders = GetDefaultHandlerFolders();

        foreach (var item in notification.SavedEntities)
        {
            await ProcessItem(notification, item, handlerFolders);
        }
    }

    /// <summary>
    ///  Export a single item in response to a notification, cleaning up any orphaned files afterwards.
    /// </summary>
    /// <remarks>
    ///  an item is only exported once per Umbraco operation - see <see cref="ClaimItemForExport(EnumerableObjectNotification{TObject}, TObject)"/>.
    /// </remarks>
    protected async Task ProcessItem(EnumerableObjectNotification<TObject> notification, TObject item, string[] handlerFolders)
    {
        if (ClaimItemForExport(notification, item) is false)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Skipping export of {name} - already exported by an earlier notification in this operation", item.Name);

            return;
        }

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

    /// <summary>
    ///  Claim an item for export, returning false if it has already been exported in this operation.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   From Umbraco 18.1 a save-and-publish raises the saved notification as well as the
    ///   published one (umbraco/Umbraco-CMS#23523), and unpublishing a culture raises the saved
    ///   and unpublished notifications. Without this, one editor action would export the item
    ///   two or three times.
    ///  </para>
    ///  <para>
    ///   All the notifications for one operation share a single notification state object, so we
    ///   track the claimed items there. The item is claimed before the export is attempted, so a
    ///   failed export isn't retried (and re-reported) by the next notification in the operation.
    ///  </para>
    ///  <para>
    ///   The first notification wins. By the time any of them are raised Umbraco has already
    ///   persisted the item - including its publish state - so the export reflects the finished
    ///   operation whichever notification triggers it.
    ///  </para>
    /// </remarks>
    internal static bool ClaimItemForExport(EnumerableObjectNotification<TObject> notification, TObject item)
    {
        var state = notification.State;

        lock (state)
        {
            if (state.TryGetValue(uSync.EventExportedItemsKey, out var value) is false
                || value is not HashSet<Guid> exported)
            {
                exported = [];
                state[uSync.EventExportedItemsKey] = exported;
            }

            return exported.Add(item.Key);
        }
    }
}
