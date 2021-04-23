using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.BackOffice.SyncHandlers
{
    public abstract class SyncHandlerBase<TObject, TService>
        : SyncHandlerRoot<TObject, IEntity>

        where TObject : IEntity
        where TService : IService
    {

        protected readonly IEntityService entityService;


        public SyncHandlerBase(
            ILogger<SyncHandlerBase<TObject, TService>> logger,
            IShortStringHelper shortStringHelper,
            uSyncConfigService uSyncConfig,
            AppCaches appCaches,
            ISyncSerializer<TObject> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService,
            IEntityService entityService)
            : base(logger, shortStringHelper, uSyncConfig, appCaches, serializer, syncItemFactory, syncFileService)
        {
            this.entityService = entityService;
        }

        protected override IEnumerable<uSyncAction> DeleteMissingItems(TObject parent, IEnumerable<Guid> keys, bool reportOnly)
        {
            var items = GetChildItems(parent.Id).ToList();

            logger.LogDebug("DeleteMissingItems: {parentId} Checking {itemCount} items for {keyCount} keys", parent.Id, items.Count, keys.Count());

            var actions = new List<uSyncAction>();
            foreach (var item in items.Where(x => !keys.Contains(x.Key)))
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
                actions.Add(
                    uSyncActionHelper<TObject>.SetAction(SyncAttempt<TObject>.Succeed(name, ChangeType.Delete), string.Empty, item.Key, this.Alias));
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
            {
                if (parent == -1)
                {
                    return entityService.GetChildren(parent, this.itemObjectType);
                }
                else
                {
                    // If you ask for the type then you get more info, and there is extra db calls to 
                    // load it, so GetChildren without the object type is quicker. 
                    return entityService.GetChildren(parent);
                }
            }

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
