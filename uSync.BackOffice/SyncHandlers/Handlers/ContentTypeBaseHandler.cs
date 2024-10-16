using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  handler base for all ContentTypeBase handlers
/// </summary>
public abstract class ContentTypeBaseHandler<TObject> : SyncHandlerContainerBase<TObject>
    where TObject : ITreeEntity
{

    /// <summary>
    /// Constructor.
    /// </summary>
    protected ContentTypeBaseHandler(
        ILogger<SyncHandlerContainerBase<TObject>> logger,
        IEntityService entityService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        SyncFileService syncFileService,
        uSyncEventService mutexService,
        uSyncConfigService uSyncConfig,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
    { }

}
