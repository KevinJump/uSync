
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;

namespace uSync.BackOffice.SyncHandlers;

/// <summary>
///  handlers that have a tree 
///  
///  for flat processing these need to preload all the files, to workout what order 
///  they go in, but that is ok because all treeSerializers store the level in the 
///  top attribute. 
/// </summary>
public abstract class SyncHandlerTreeBase<TObject> : SyncHandlerLevelBase<TObject>
    where TObject : ITreeEntity
{
    /// <inheritdoc/>
    protected SyncHandlerTreeBase(
        ILogger<SyncHandlerTreeBase<TObject>> logger,
        IEntityService entityService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
    { }

    /// <inheritdoc/>
    protected override string GetItemName(TObject item) => item.Name ?? item.Id.ToString();

    /// <inheritdoc/>
    protected override bool DoItemsMatch(XElement node, TObject item)
    {
        if (item.Key == node.GetKey()) return true;

        // in a tree items can have the same alias in different places.
        // so we only do this match on key.
        return false;


    }
}
