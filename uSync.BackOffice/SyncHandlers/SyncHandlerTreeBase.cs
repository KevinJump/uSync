using System;

using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Strings;

using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    ///  handlers that have a tree 
    ///  
    ///  for flat processing these need to preload all the files, to workout what order 
    ///  they go in, but that is ok because all treeSerializers store the level in the 
    ///  top attribute. 
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TService"></typeparam>
    public abstract class SyncHandlerTreeBase<TObject, TService> : SyncHandlerLevelBase<TObject, TService>
        where TObject : ITreeEntity
        where TService : IService
    {
        protected SyncHandlerTreeBase(
            IShortStringHelper shortStringHelper,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<TObject> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(shortStringHelper, entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
        { }
        
        protected override string GetItemName(TObject item) => item.Name;
    }

}
