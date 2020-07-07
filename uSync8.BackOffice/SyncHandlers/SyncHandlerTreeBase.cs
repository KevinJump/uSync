using System;
using System.Linq;

using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

using uSync8.BackOffice.Services;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.BackOffice.SyncHandlers
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
    public abstract class SyncHandlerTreeBase<TObject> : SyncHandlerLevelBase<TObject>
        where TObject : ITreeEntity
    {
        protected SyncHandlerTreeBase(
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<TObject> serializer,
            SyncTrackerCollection trackers,
            SyncDependencyCollection checkers,
            SyncFileService syncFileService)
            : base(entityService, logger, appCaches, serializer, trackers, checkers, syncFileService)
        { }

        protected override string GetItemName(TObject item) => item.Name;
    }

    [Obsolete]
    public abstract class SyncHandlerTreeBase<TObject, TService> : SyncHandlerTreeBase<TObject>
        where TObject : ITreeEntity
    {
        [Obsolete("Handler should take tracker and dependency checkers for completeness.")]
        protected SyncHandlerTreeBase(
                IEntityService entityService,
                IProfilingLogger logger,
                ISyncSerializer<TObject> serializer,
                ISyncTracker<TObject> tracker,
                AppCaches appCaches,
                SyncFileService syncFileService)
                : base(entityService, logger, appCaches, serializer, null, null, syncFileService)
        {
            logger.Debug(handlerType, "Obsolete Call SyncHandlerTreeBase1");
        }

        [Obsolete("Construct your handler using the tracker & Dependecy collections for better checker support")]
        protected SyncHandlerTreeBase(
                   IEntityService entityService,
                   IProfilingLogger logger,
                   ISyncSerializer<TObject> serializer,
                   ISyncTracker<TObject> tracker,
                   AppCaches appCaches,
                   ISyncDependencyChecker<TObject> checker,
                   SyncFileService syncFileService)
                   : base(entityService, logger, appCaches, serializer, null, null, syncFileService)
        {
            logger.Debug(handlerType, "Obsolete Call SyncHandlerTreeBase2");
        }

        protected SyncHandlerTreeBase(IEntityService entityService, IProfilingLogger logger, AppCaches appCaches, ISyncSerializer<TObject> serializer, SyncTrackerCollection trackers, SyncDependencyCollection checkers, SyncFileService syncFileService)
            : base(entityService, logger, appCaches, serializer, trackers, checkers, syncFileService)
        { }
    }

}
