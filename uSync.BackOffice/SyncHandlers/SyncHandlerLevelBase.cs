using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Models;

namespace uSync.BackOffice.SyncHandlers;

/// <summary>
///  SyncHandler for things that have a level, 
///  
/// ideally this would be in SyncHandlerTreeBase, but 
/// Templates have levels but are not ITreeEntities 
/// </summary>
public abstract class SyncHandlerLevelBase<TObject>
    : SyncHandlerBase<TObject>
    where TObject : IEntity
{
    /// <inheritdoc/>
    protected SyncHandlerLevelBase(
        ILogger<SyncHandlerLevelBase<TObject>> logger,
        IEntityService entityService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
    { }

    /// <summary>
    ///  Sorts the loaded items. 
    /// </summary>
    /// <remarks>
    ///  as we are already loading everything to merge, it doesn't
    ///  then cost us much to sort them when we have to.
    /// </remarks>
    protected override async Task<IReadOnlyList<OrderedNodeInfo>> GetMergedItemsAsync(string[] folders)
        => [.. (await base.GetMergedItemsAsync(folders)).OrderBy(x => x.Level)];

    /// <inheritdoc/>
    override protected string GetItemPath(TObject item, bool useGuid, bool isFlat)
    {
        if (isFlat) return base.GetItemPath(item, useGuid, isFlat);
        return GetEntityTreePath((IUmbracoEntity)item, useGuid, true);
    }

    /// <summary>
    ///  get the tree path for an item (eg. /homepage/about-us/something )
    /// </summary>
    /// <param name="item"></param>
    /// <param name="useGuid"></param>
    /// <param name="isTop"></param>
    /// <returns></returns>
    protected string GetEntityTreePath(IUmbracoEntity item, bool useGuid, bool isTop)
    {
        var path = string.Empty;
        if (item != null)
        {
            if (item.ParentId > 0)
            {
                var parent = this.itemFactory.EntityCache.GetEntity(item.ParentId);
                // var parent = entityService.Get(item.ParentId);
                if (parent != null)
                {
                    path = GetEntityTreePath(parent, useGuid, false);
                }
            }

            // we only want the guid file name at the top of the tree 
            path = Path.Combine(path, GetEntityTreeName(item, useGuid && isTop));
        }

        return path;
    }

    /// <summary>
    ///  the name of an item in an entity tree 
    /// </summary>
    virtual protected string GetEntityTreeName(IUmbracoEntity item, bool useGuid)
    {
        if (item is not null)
        {
            if (useGuid) return item.Key.ToString();
            return item.Name?.ToSafeFileName(shortStringHelper) ?? Guid.NewGuid().ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
