using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.Core;
using uSync.Core.Models;

namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    ///  Base class for any Handlers that manage IEntity type objects
    /// </summary>
    public abstract class SyncHandlerBase<TObject, TService>
        : SyncHandlerRoot<TObject, IEntity>, ISyncCleanEntryHandler
        where TObject : IEntity
        where TService : IService
    {

        /// <summary>
        /// reference to Umbraco Entity service
        /// </summary>
        protected readonly IEntityService entityService;


        /// <inheritdoc/>
        public SyncHandlerBase(
            ILogger<SyncHandlerBase<TObject, TService>> logger,
            IEntityService entityService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncItemFactory syncItemFactory)
            : base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
        {
            this.entityService = entityService;
        }

        protected override bool HasChildren(TObject item)
            => entityService.GetChildren(item.Id).Any();

        /// <summary>
        ///  given a folder we calculate what items we can remove, becuase they are 
        ///  not in one the the files in the folder.
        /// </summary>
        protected override IEnumerable<uSyncAction> CleanFolder(string cleanFile, bool reportOnly, bool flat)
        {
            var folder = Path.GetDirectoryName(cleanFile);
            if (!Directory.Exists(folder)) return Enumerable.Empty<uSyncAction>();


            // get the keys for every item in this folder. 

            // this would works on the flat folder stucture too, 
            // there we are being super defensive, so if an item
            // is anywhere in the folder it won't get removed
            // even if the folder is wrong
            // be a little slower (not much though)

            // we cache this, (it is cleared on an ImportAll)
            var keys = GetFolderKeys(folder, flat);
            if (keys.Count > 0)
            {
                // move parent to here, we only need to check it if there are files.
                var parentId = GetCleanParentId(cleanFile);
                if (parentId == 0) return Enumerable.Empty<uSyncAction>();

                logger.LogDebug("Got parent with {Id} from clean file {file}", parentId, Path.GetFileName(cleanFile));

                // keys should aways have at least one entry (the key from cleanFile)
                // if it doesn't then something might have gone wrong.
                // because we are being defensive when it comes to deletes, 
                // we only then do deletes when we know we have loaded some keys!
                return DeleteMissingItems(parentId, keys, reportOnly);
            }
            else
            {
                logger.LogWarning("Failed to get the keys for items in the folder, there might be a disk issue {folder}", folder);
                return Enumerable.Empty<uSyncAction>();
            }
        }

        private int GetCleanParentId(string cleanFile)
        {
            var node = XElement.Load(cleanFile);
            var id = node.Attribute("Id").ValueOrDefault(0);
            if (id != 0) return id;
            return GetCleanParent(cleanFile)?.Id ?? 0;
        }

        /// <summary>
        ///  Process any cleanup actions that may have been loaded up
        /// </summary>
        public virtual IEnumerable<uSyncAction> ProcessCleanActions(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
        {
            var cleans = actions.Where(x => x.Change == ChangeType.Clean && !string.IsNullOrWhiteSpace(x.FileName)).ToList();
            if (cleans.Count == 0) return Enumerable.Empty<uSyncAction>();

            var results = new List<uSyncAction>();

            foreach (var clean in cleans)
            {
                if (!string.IsNullOrWhiteSpace(clean.FileName))
                    results.AddRange(CleanFolder(clean.FileName, false, config.UseFlatStructure));
            }

            return results;
        }

        /// <inheritdoc/>
        protected override IEnumerable<uSyncAction> DeleteMissingItems(TObject parent, IEnumerable<Guid> keys, bool reportOnly)
            => DeleteMissingItems(parent?.Id ?? 0, keys, reportOnly);

        /// <inheritdoc/>
        protected override IEnumerable<uSyncAction> DeleteMissingItems(int parentId, IEnumerable<Guid> keys, bool reportOnly)
        {
            var items = GetChildItems(parentId).ToList();

            logger.LogDebug("DeleteMissingItems: {parentId} Checking {itemCount} items for {keyCount} keys", parentId, items.Count, keys.Count());

            var actions = new List<uSyncAction>();
            foreach (var item in items.Where(x => !keys.Contains(x.Key)))
            {
                logger.LogDebug("DeleteMissingItems: Found {item} that is not in file list (Reporting: {reportOnly})", item.Id, reportOnly);

                var name = String.Empty;
                if (item is IEntitySlim slim) name = slim.Name;

                if (string.IsNullOrEmpty(name) || !reportOnly)
                {
                    var actualItem = GetFromService(item.Id);
                    if (actualItem == null)
                    {
                        logger.LogDebug("Actual Item {id} can't be found", item.Id);
                        continue;
                    }

                    name = GetItemName(actualItem);

                    // actually do the delete if we are really not reporting
                    if (!reportOnly)
                    {
                        logger.LogInformation("Deleting item: {id} {name} as part of a 'clean' import", actualItem.Id, name);
                        DeleteViaService(actualItem);
                    }
                }

                // for reporting - we use the entity name,
                // this stops an extra lookup - which we may not need later
                actions.Add(
                    uSyncActionHelper<TObject>.SetAction(SyncAttempt<TObject>.Succeed(name, ChangeType.Delete), string.Empty, item.Key, this.Alias));
            }

            return actions;
        }

        /// <summary>
        /// Export all items under a suppled parent id
        /// </summary>
        virtual public IEnumerable<uSyncAction> ExportAll(int parentId, string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            var parent = GetFromService(parentId);
            return ExportAll(parent, folder, config, callback);
        }

        /// <inheritdoc/>
        protected override IEnumerable<IEntity> GetChildItems(IEntity parent)
        {
            if (parent == null) return GetChildItems(-1);
            return GetChildItems(parent.Id);
        }

        // almost everything does this - but languages can't so we need to 
        // let the language Handler override this. 

        /// <summary>
        ///  Get all child items beneath a given item
        /// </summary>
        virtual protected IEnumerable<IEntity> GetChildItems(int parent)
        {
            if (this.itemObjectType == UmbracoObjectTypes.Unknown)
                return Enumerable.Empty<IEntity>();

            var cacheKey = $"{GetCacheKeyBase()}_parent_{parent}";

            return runtimeCache.GetCacheItem(cacheKey, () =>
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
            }, null);
        }

        /// <summary>
        /// Get all 'folders' beneath a given item (usally these are Container items)
        /// </summary>
        virtual protected IEnumerable<IEntity> GetFolders(int parent)
        {
            if (this.itemContainerType != UmbracoObjectTypes.Unknown)
                return entityService.GetChildren(parent, this.itemContainerType);

            return Enumerable.Empty<IEntity>();
        }

        /// <inheritdoc/>
        protected override IEnumerable<IEntity> GetFolders(IEntity parent)
        {
            if (parent == null) return GetFolders(-1);
            return GetFolders(parent.Id);
        }

        /// <inheritdoc/>
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
