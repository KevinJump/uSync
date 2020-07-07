using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.BackOffice.SyncHandlers
{
    public abstract class SyncHandlerBase<TObject> : SyncHandlerRoot<TObject, IEntity>
        where TObject : IEntity
    {
        protected readonly IEntityService entityService;

        /// <summary>
        ///  Prefered constructor, uses collections to load trackers and checkers. 
        /// </summary>
        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<TObject> serializer,
            SyncTrackerCollection trackers,
            SyncDependencyCollection checkers,
            SyncFileService syncFileService)
            : base(logger, appCaches, serializer, trackers, checkers, syncFileService)
        {
            this.entityService = entityService;
        }

        /// <summary>
        ///  Remove items that are child of parent and not in the list of supplied keys
        /// </summary>
        protected override IEnumerable<uSyncAction> DeleteMissingItems(TObject parent, IEnumerable<Guid> keys, bool reportOnly)
        {
            var itemsToRemove = GetChildItems(parent)
                .Where(x => !keys.Contains(x.Key))
                .ToList();

            var actions = new List<uSyncAction>();
            foreach (var item in itemsToRemove)
            {
                var name = String.Empty;
                if (item is IEntitySlim slim) name = slim.Name;

                if (string.IsNullOrEmpty(name) || !reportOnly)
                {
                    var actualItem = GetFromService(item.Key);
                    name = GetItemName(actualItem);

                    // actually do the delete if we are really not reporting
                    if (!reportOnly) DeleteViaService(actualItem);
                }

                // for reporting - we use the entity name,
                // this stops an extra lookup - which we may not need later
                actions.Add(uSyncActionHelper<TObject>.SetAction(SyncAttempt<TObject>.Succeed(name, ChangeType.Delete), string.Empty));
            }

            return actions;
        }

        // Functions catering for the fact that all our items are IEntity based
        protected override Guid GetItemKey(TObject item) => item.Key;
        protected override TObject GetFromService(IEntity baseItem) => GetFromService(baseItem.Id);

        /// <summary>
        ///  gets a container item
        /// </summary>
        /// <remarks>
        ///  container items sometimes are diffrent to actual items (e.g doctype containers)
        /// </remarks>
        protected override IEntity GetContainer(Guid key) => GetFromService(key);

        // almost everything does it this way - but languages can't so we need to 
        // let the language Handler override this. 
        protected override IEnumerable<IEntity> GetChildItems(IEntity parent)
        {
            if (parent == null) return GetChildItems(-1);
            return GetChildItems(parent.Id);
        }

        protected virtual IEnumerable<IEntity> GetChildItems(int id)
        {
            if (this.itemObjectType != UmbracoObjectTypes.Unknown)
                return entityService.GetChildren(id, this.itemObjectType);

            return Enumerable.Empty<IEntity>();
        }

        protected override IEnumerable<IEntity> GetFolders(IEntity parent)
        {
            if (parent == null) return GetFolders(-1);
            return GetFolders(parent.Id);
        }

        protected virtual IEnumerable<IEntity> GetFolders(int id)
        {
            if (this.itemContainerType != UmbracoObjectTypes.Unknown)
                return entityService.GetChildren(id, this.itemContainerType);

            return Enumerable.Empty<IEntity>();
        }

        public virtual IEnumerable<uSyncAction> ExportAll(int parentId, string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            var parent = GetFromService(parentId);
            return ExportAll(parent, folder, config, callback);
        }
    }

    [Obsolete]
    public abstract class SyncHandlerBase<TObject, TService>
        : SyncHandlerBase<TObject>
        where TObject : IEntity
    {
        [Obsolete("Construct your handler using the tracker & Dependecy collections for better checker support")]
        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            SyncFileService syncFileService)
            : base(entityService, logger, appCaches, serializer, default, default, syncFileService)
        { }

        [Obsolete("Construct your handler using the tracker & Dependecy collections for better checker support")]
        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            ISyncDependencyChecker<TObject> dependencyChecker,
            SyncFileService syncFileService)
            : base(entityService, logger, appCaches, serializer, default, default, syncFileService)
        { }

        protected SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<TObject> serializer,
            SyncTrackerCollection trackers,
            SyncDependencyCollection checkers,
            SyncFileService syncFileService)
            : base(entityService, logger, appCaches, serializer, trackers, checkers, syncFileService)
        { }

    }

}