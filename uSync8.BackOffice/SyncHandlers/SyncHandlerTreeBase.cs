﻿using System;

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
    public abstract class SyncHandlerTreeBase<TObject, TService> : SyncHandlerLevelBase<TObject, TService>
        where TObject : ITreeEntity
        where TService : IService
    {
        protected SyncHandlerTreeBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, appCaches, syncFileService)
        {
        }
        protected SyncHandlerTreeBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            SyncTrackerCollection trackers,
            AppCaches appCaches,
            SyncDependencyCollection checkers,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, trackers, appCaches, checkers, syncFileService)
        {
        }

        [Obsolete("Construct your handler using SyncDependencyCollection for better checker support")]
        protected SyncHandlerTreeBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            ISyncDependencyChecker<TObject> checker,
            SyncFileService fileService)
            : base(entityService, logger, serializer, tracker, appCaches, checker, fileService)
        {

        }




        protected override string GetItemName(TObject item) => item.Name;
    }

}
