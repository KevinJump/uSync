using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;

using uSync.Core;

namespace uSync.BackOffice.Cache;

/// <summary>
///  listens to Umbraco and forgets anything the state cache can no longer vouch for.
/// </summary>
/// <remarks>
///  <para>
///   this is what stops the cache going stale: as soon as Umbraco tells us an item has been
///   saved, deleted, moved or published, we stop claiming to know anything about it. all of it
///   happens whether or not the cache is currently switched on, because otherwise turning it
///   off, editing things, and turning it back on would leave the cache confidently wrong.
///  </para>
///  <para>
///   three levels of forgetting, and picking the right one is the whole game:
///   <list type="bullet">
///    <item><b>the item</b> - a save or publish only changes that item's own xml.</item>
///    <item><b>every item of that type</b> - a move rewrites the path of everything underneath
///          it, and paths are part of the serialized xml.</item>
///    <item><b>everything</b> - doc types, data types, templates and languages all get embedded
///          in other items' xml, so changing one silently changes items whose own rows never
///          moved. handled inside the cache itself (see SyncStateCache's shared item types), so
///          it applies however the invalidation arrives.</item>
///   </list>
///  </para>
///  <para>
///   note there is no check for uSync being paused here. during uSync's own import, an item we
///   write is invalidated and then simply not re-recorded (we only record confirmed "no change"
///   results), so invalidating is both harmless and the safer default - it also catches items
///   Umbraco saves as a side effect of something we imported.
///  </para>
/// </remarks>
internal class SyncStateCacheInvalidator :

    // content, media and elements - the high volume types, invalidated one item at a time.
    INotificationAsyncHandler<ContentSavedNotification>,
    INotificationAsyncHandler<ContentDeletedNotification>,
    INotificationAsyncHandler<ContentPublishedNotification>,
    INotificationAsyncHandler<ContentUnpublishedNotification>,
    INotificationAsyncHandler<ContentSavedBlueprintNotification>,
    INotificationAsyncHandler<ContentDeletedBlueprintNotification>,
    INotificationAsyncHandler<MediaSavedNotification>,
    INotificationAsyncHandler<MediaDeletedNotification>,
    INotificationAsyncHandler<ElementSavedNotification>,
    INotificationAsyncHandler<ElementDeletedNotification>,
    INotificationAsyncHandler<ElementPublishedNotification>,
    INotificationAsyncHandler<ElementUnpublishedNotification>,
    INotificationAsyncHandler<DictionaryItemSavedNotification>,
    INotificationAsyncHandler<DictionaryItemDeletedNotification>,

    // moves change the path of every descendant, so the whole type has to go.
    INotificationAsyncHandler<ContentMovedNotification>,
    INotificationAsyncHandler<ContentMovedToRecycleBinNotification>,
    INotificationAsyncHandler<MediaMovedNotification>,
    INotificationAsyncHandler<MediaMovedToRecycleBinNotification>,
    INotificationAsyncHandler<ElementMovedNotification>,
    INotificationAsyncHandler<ElementMovedToRecycleBinNotification>,

    // low volume types - not worth the per-item bookkeeping, so we drop the type.
    INotificationAsyncHandler<DomainSavedNotification>,
    INotificationAsyncHandler<DomainDeletedNotification>,
    INotificationAsyncHandler<RelationTypeSavedNotification>,
    INotificationAsyncHandler<RelationTypeDeletedNotification>,
    INotificationAsyncHandler<WebhookSavedNotification>,
    INotificationAsyncHandler<WebhookDeletedNotification>,

    // types that get embedded in other items - these all end up clearing everything.
    INotificationAsyncHandler<ContentTypeSavedNotification>,
    INotificationAsyncHandler<ContentTypeDeletedNotification>,
    INotificationAsyncHandler<ContentTypeMovedNotification>,
    INotificationAsyncHandler<MediaTypeSavedNotification>,
    INotificationAsyncHandler<MediaTypeDeletedNotification>,
    INotificationAsyncHandler<MediaTypeMovedNotification>,
    INotificationAsyncHandler<MemberTypeSavedNotification>,
    INotificationAsyncHandler<MemberTypeDeletedNotification>,
    INotificationAsyncHandler<MemberTypeMovedNotification>,
    INotificationAsyncHandler<DataTypeSavedNotification>,
    INotificationAsyncHandler<DataTypeDeletedNotification>,
    INotificationAsyncHandler<DataTypeMovedNotification>,
    INotificationAsyncHandler<TemplateSavedNotification>,
    INotificationAsyncHandler<TemplateDeletedNotification>,
    INotificationAsyncHandler<LanguageSavedNotification>,
    INotificationAsyncHandler<LanguageDeletedNotification>,
    INotificationAsyncHandler<EntityContainerSavedNotification>,
    INotificationAsyncHandler<EntityContainerRenamedNotification>
{
    private readonly ISyncStateCache _stateCache;

    public SyncStateCacheInvalidator(ISyncStateCache stateCache)
    {
        _stateCache = stateCache;
    }

    #region Content

    /// <inheritdoc/>
    public Task HandleAsync(ContentSavedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Content, n.SavedEntities);

    /// <inheritdoc/>
    public Task HandleAsync(ContentDeletedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Content, n.DeletedEntities);

    /// <inheritdoc/>
    /// <remarks>publishing does not always fire a save, so it needs handling separately.</remarks>
    public Task HandleAsync(ContentPublishedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Content, n.PublishedEntities);

    /// <inheritdoc/>
    public Task HandleAsync(ContentUnpublishedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Content, n.UnpublishedEntities);

    /// <inheritdoc/>
    /// <remarks>blueprints are serialized as content, so they share the same entry keys.</remarks>
    public Task HandleAsync(ContentSavedBlueprintNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Content, [n.SavedBlueprint]);

    /// <inheritdoc/>
    public Task HandleAsync(ContentDeletedBlueprintNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Content, n.DeletedBlueprints);

    /// <inheritdoc/>
    public Task HandleAsync(ContentMovedNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.Content);

    /// <inheritdoc/>
    public Task HandleAsync(ContentMovedToRecycleBinNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.Content);

    #endregion

    #region Media

    /// <inheritdoc/>
    public Task HandleAsync(MediaSavedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Media, n.SavedEntities);

    /// <inheritdoc/>
    public Task HandleAsync(MediaDeletedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Media, n.DeletedEntities);

    /// <inheritdoc/>
    public Task HandleAsync(MediaMovedNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.Media);

    /// <inheritdoc/>
    public Task HandleAsync(MediaMovedToRecycleBinNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.Media);

    #endregion

    #region Elements

    /// <inheritdoc/>
    public Task HandleAsync(ElementSavedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Element, n.SavedEntities);

    /// <inheritdoc/>
    public Task HandleAsync(ElementDeletedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Element, n.DeletedEntities);

    /// <inheritdoc/>
    public Task HandleAsync(ElementPublishedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Element, n.PublishedEntities);

    /// <inheritdoc/>
    public Task HandleAsync(ElementUnpublishedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Element, n.UnpublishedEntities);

    /// <inheritdoc/>
    public Task HandleAsync(ElementMovedNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.Element);

    /// <inheritdoc/>
    public Task HandleAsync(ElementMovedToRecycleBinNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.Element);

    #endregion

    #region Dictionary

    /// <inheritdoc/>
    public Task HandleAsync(DictionaryItemSavedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Dictionary, n.SavedEntities);

    /// <inheritdoc/>
    public Task HandleAsync(DictionaryItemDeletedNotification n, CancellationToken c)
        => InvalidateAsync(Core.uSyncConstants.Serialization.Dictionary, n.DeletedEntities);

    #endregion

    #region Low volume types (drop the whole type, there is nothing to gain by being precise)

    /// <inheritdoc/>
    public Task HandleAsync(DomainSavedNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.Domain);

    /// <inheritdoc/>
    public Task HandleAsync(DomainDeletedNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.Domain);

    /// <inheritdoc/>
    public Task HandleAsync(RelationTypeSavedNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.RelationType);

    /// <inheritdoc/>
    public Task HandleAsync(RelationTypeDeletedNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.RelationType);

    /// <inheritdoc/>
    public Task HandleAsync(WebhookSavedNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.Webhook);

    /// <inheritdoc/>
    public Task HandleAsync(WebhookDeletedNotification n, CancellationToken c)
        => _stateCache.InvalidateTypeAsync(Core.uSyncConstants.Serialization.Webhook);

    #endregion

    #region Types that change how other items serialize

    // all of these route through Invalidate with an item type the cache treats as shared,
    // which clears everything. keeping them as normal invalidations (rather than calling
    // InvalidateAll here) means the policy lives in one place.

    /// <inheritdoc/>
    public Task HandleAsync(ContentTypeSavedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(ContentTypeDeletedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(ContentTypeMovedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(MediaTypeSavedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(MediaTypeDeletedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(MediaTypeMovedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(MemberTypeSavedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(MemberTypeDeletedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(MemberTypeMovedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(DataTypeSavedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(DataTypeDeletedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(DataTypeMovedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(TemplateSavedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(TemplateDeletedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(LanguageSavedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(LanguageDeletedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    /// <remarks>a container is a folder - renaming one changes the path of everything in it.</remarks>
    public Task HandleAsync(EntityContainerSavedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    /// <inheritdoc/>
    public Task HandleAsync(EntityContainerRenamedNotification n, CancellationToken c) => InvalidateEverythingAsync();

    private Task InvalidateEverythingAsync()
        => _stateCache.InvalidateAllAsync();

    #endregion

    private async Task InvalidateAsync(string itemType, IEnumerable<IEntity> items)
    {
        if (items is null) return;

        foreach (var item in items.Where(x => x is not null))
        {
            await _stateCache.InvalidateAsync(itemType, item.Key);
        }
    }
}
