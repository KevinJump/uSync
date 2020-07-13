﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Umbraco.Core.Cache;
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
    public abstract class SyncHandlerBase<TObject, TService>
        : SyncHandlerRoot<TObject, IEntity>

        where TObject : IEntity
        where TService : IService
    {

        protected readonly IEntityService entityService;

        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            SyncFileService syncFileService)
        : this(entityService, logger, serializer, tracker, appCaches, null, syncFileService) { }


        public SyncHandlerBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            ISyncDependencyChecker<TObject> dependencyChecker,
            SyncFileService syncFileService)
            : base(logger,serializer,tracker, appCaches, dependencyChecker, syncFileService)
        {
            this.entityService = entityService;
        }
      
        protected override IEnumerable<uSyncAction> DeleteMissingItems(TObject parent, IEnumerable<Guid> keys, bool reportOnly)
        {
            var items = GetChildItems(parent.Id).ToList();
            var actions = new List<uSyncAction>();
            foreach (var item in items)
            {
                if (!keys.Contains(item.Key))
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
            }

            return actions;
        }


        virtual public IEnumerable<uSyncAction> ExportAll(int parentId, string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            var parent = GetFromService(parentId);
            return ExportAll(parent, folder, config, callback);
        }


        protected override IEnumerable<IEntity> GetChildItems(IEntity parent)
        {
            if (parent == null) return GetChildItems(-1);
            return GetChildItems(parent.Id);
        }

        // almost everything does this - but languages can't so we need to 
        // let the language Handler override this. 
        virtual protected IEnumerable<IEntity> GetChildItems(int parent)
        {
            if (this.itemObjectType != UmbracoObjectTypes.Unknown)
                return entityService.GetChildren(parent, this.itemObjectType);

            return Enumerable.Empty<IEntity>();
        }

        virtual protected IEnumerable<IEntity> GetFolders(int parent)
        {
            if (this.itemContainerType != UmbracoObjectTypes.Unknown)
                return entityService.GetChildren(parent, this.itemContainerType);

            return Enumerable.Empty<IEntity>();
        }

        protected override IEnumerable<IEntity> GetFolders(IEntity parent)
        {
            if (parent == null) return GetFolders(-1);
            return GetFolders(parent.Id);
        }

        protected override string GetItemAlias(TObject item)
            => GetItemName(item);

        protected override Guid GetItemKey(TObject item) => item.Key;

        protected override TObject GetFromService(IEntity entity)
            => GetFromService(entity.Id);

        /// <summary>
        ///  for backwards compatability up the tree.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool HasChildren(int id)
            => GetFolders(id).Any() || GetChildItems(id).Any();

    }
}
