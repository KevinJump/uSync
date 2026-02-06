using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.Core;

namespace uSync.Extend;

/// <summary>
///  object based handler, this is the most basic handler to implement, 
///  you just need to return all your items and then uSync will do the rest.
/// </summary>
public abstract class SyncObjectHandler<TObject> : SyncHandlerRoot<TObject, TObject>, ISyncHandler
{
    protected SyncObjectHandler(
        ILogger<SyncHandlerRoot<TObject, TObject>> logger,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory itemFactory) 
        : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
    { }

    protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(TObject parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
    {
        // you don't have to support deleing missing items, but if you do, this is where you would do it.
        // You would get all the items that are children of the parent, and then compare them to the keysToKeep.
        // If there are any items that are not in the keysToKeep, then you would delete them.
        // If reportOnly is true, then you would just return a list of actions that would be taken,
        // but not actually perform the deletions.
        return Task.FromResult(Enumerable.Empty<uSyncAction>());
    }

    protected override Task<IEnumerable<TObject>> GetChildItemsAsync(TObject? parent)
    {
        if (parent is not null) return Task.FromResult(Enumerable.Empty<TObject>());
        return GetAllItems();
    }

    /// <summary>
    ///  a container (folder) item, is not supported in this handler,
    ///  so we return null to indicate that the item is not found.
    /// </summary>
    protected override Task<TObject?> GetFromServiceAsync(TObject? item) => 
        Task.FromResult(default(TObject));

    protected abstract Task<IEnumerable<TObject>> GetAllItems();

    /// <summary>
    ///  we are not supporing folders this handler assumes its all flat. 
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    protected override Task<IEnumerable<TObject>> GetFoldersAsync(TObject? parent) => 
        Task.FromResult(Enumerable.Empty<TObject>());
}
