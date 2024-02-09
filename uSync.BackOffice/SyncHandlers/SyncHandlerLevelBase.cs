using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Models;

namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    ///  SyncHandler for things that have a level, 
    ///  
    /// ideally this would be in SyncHandlerTreeBase, but 
    /// Templates have levels but are not ITreeEntities 
    /// </summary>
    public abstract class SyncHandlerLevelBase<TObject, TService>
        : SyncHandlerBase<TObject, TService>
        where TObject : IEntity
        where TService : IService
    {
        /// <inheritdoc/>
        protected SyncHandlerLevelBase(
            ILogger<SyncHandlerLevelBase<TObject, TService>> logger,
            IEntityService entityService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
        { }

        /// <summary>
        ///  Sorts the loaded items. 
        /// </summary>
        /// <remarks>
        ///  as we are already loading everything to merge, it doesn't
        ///  then cost us much to sort them when we have to.
        /// </remarks>
        protected override IReadOnlyList<OrderedNodeInfo> GetMergedItems(string[] folders)
            => base.GetMergedItems(folders).OrderBy(x => x.Level).ToList();
                
       
        /// <inheritdoc/>
        override protected string GetItemPath(TObject item, bool useGuid, bool isFlat)
        {
            if (isFlat) return base.GetItemPath(item, useGuid, isFlat);
            return GetEntityTreePath((IUmbracoEntity)item, useGuid, true);
        }

        /// <summary>
        ///  get the tree path for an item (eg. /homepage/about-us/something )
        /// </summary>
        /// <param name="item"></param>
        /// <param name="useGuid"></param>
        /// <param name="isTop"></param>
        /// <returns></returns>
        protected string GetEntityTreePath(IUmbracoEntity item, bool useGuid, bool isTop)
        {
            var path = string.Empty;
            if (item != null)
            {
                if (item.ParentId > 0)
                {
                    var parent = this.itemFactory.EntityCache.GetEntity(item.ParentId);
                    // var parent = entityService.Get(item.ParentId);
                    if (parent != null)
                    {
                        path = GetEntityTreePath(parent, useGuid, false);
                    }
                }

                // we only want the guid file name at the top of the tree 
                path = Path.Combine(path, GetEntityTreeName(item, useGuid && isTop));
            }

            return path;
        }

        /// <summary>
        ///  the name of an item in an entity tree 
        /// </summary>
        virtual protected string GetEntityTreeName(IUmbracoEntity item, bool useGuid)
        {
            if (item != null)
            {
                if (useGuid) return item.Key.ToString();
                return item.Name.ToSafeFileName(shortStringHelper);
            }

            return Guid.NewGuid().ToString();
        }


        /// <summary>
        ///  object representing a file and its level
        /// </summary>
        [Obsolete("Items Will not be loaded using this object, to be removed v15")]
        protected class LeveledFile
        {
            /// <summary>
            ///  umbraco alias of the item
            /// </summary>
            public string Alias { get; set; }

            /// <summary>
            ///  level (e.g 0 is root) of file
            /// </summary>
            public int Level { get; set; }

            /// <summary>
            ///  path to the actual file.
            /// </summary>
            public string File { get; set; }
        }

        /// <summary>
        ///  Load a XElement node for a given path.
        /// </summary>
        [Obsolete("Items Will not be loaded using this object, to be removed v15")]
        protected XElement LoadNode(string path)
        {
            syncFileService.EnsureFileExists(path);

            using (var stream = syncFileService.OpenRead(path))
            {
                return XElement.Load(stream);
            }
        }

        /// <summary>
        ///  Get all the files in a folder and return them sorted by their level 
        /// </summary>
        [Obsolete("Items Will not be loaded using this object, to be removed v15")]
        protected virtual IList<LeveledFile> GetLevelOrderedFiles(string folder, IList<uSyncAction> actions)
        {
            List<LeveledFile> nodes = new List<LeveledFile>();

            var files = syncFileService.GetFiles(folder, $"*.{this.uSyncConfig.Settings.DefaultExtension}");
            foreach (var file in files)
            {
                try
                {
                    var node = LoadNode(file);
                    if (node != null)
                    {
                        nodes.Add(new LeveledFile
                        {
                            Alias = node.GetAlias(),
                            Level = (node.GetLevel() * 1000) + node.GetItemSortOrder(), // will hopefully let us put things in sort order in one go. 
                            File = file
                        });
                    }
                }
                catch (XmlException ex)
                {
                    // one of the files is wrong. (do we stop or carry on)
                    logger.LogWarning($"Error loading file: {file} [{ex.Message}]");
                    actions.Add(uSyncActionHelper<TObject>.SetAction(
                        SyncAttempt<TObject>.Fail(Path.GetFileName(file), ChangeType.Fail, $"Failed to Load: {ex.Message}"), file, Guid.Empty, this.Alias, false));
                }
            }

            return nodes.OrderBy(x => x.Level).ToList();
        }


    }

}
