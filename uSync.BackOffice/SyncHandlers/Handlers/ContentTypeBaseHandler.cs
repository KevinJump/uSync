using System.Linq;
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

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    /// <summary>
    ///  handler base for all ContentTypeBase handlers
    /// </summary>
    public abstract class ContentTypeBaseHandler<TObject, TService> : SyncHandlerContainerBase<TObject, TService>
        where TObject : ITreeEntity
        where TService : IService

    {

        /// <summary>
        /// Constructor.
        /// </summary>
        protected ContentTypeBaseHandler(
            ILogger<SyncHandlerContainerBase<TObject, TService>> logger,
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
}
