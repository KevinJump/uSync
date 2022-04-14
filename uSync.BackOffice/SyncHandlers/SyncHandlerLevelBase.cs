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
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;

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

        /// <inheritdoc/>
        protected override IEnumerable<uSyncAction> ImportFolder(string folder, HandlerSettings config, Dictionary<string, TObject> updates, bool force, SyncUpdateCallback callback)
        {
            // if not using flat then directory structure is sorting them for us. 
            if (config.UseFlatStructure == false)
                return base.ImportFolder(folder, config, updates, force, callback);

            List<uSyncAction> actions = new List<uSyncAction>();

            callback?.Invoke("Calculating import order", 0, 1);
            logger.LogDebug("Calculating import order");

            var orderedFiles = GetLevelOrderedFiles(folder, actions);

            // process.
            var flags = SerializerFlags.None;
            if (force) flags |= SerializerFlags.Force;

            var cleanMarkers = new List<string>();

            foreach (var item in orderedFiles.Select((Node, Index) => new { Node, Index }))
            {
                var filename = Path.GetFileNameWithoutExtension(item.Node.File);
                callback?.Invoke($"{filename}", item.Index, orderedFiles.Count);

                logger.LogTrace("{Index} Importing: {File}, [Level {Level}]", item.Index, filename, item.Node.Level);

                var result = Import(item.Node.File, config, flags);
                foreach (var attempt in result)
                {
                    if (attempt.Success)
                    {
                        if (attempt.Change == ChangeType.Clean)
                        {
                            cleanMarkers.Add(item.Node.File);
                        }
                        else if (attempt.Item != null && attempt.Item is TObject attemptItem)
                        {
                            updates.Add(item.Node.File, attemptItem);
                        }
                    }

                    if (attempt.Change != ChangeType.Clean)
                        actions.Add(attempt);
                }
            }

            if (flags.HasFlag(SerializerFlags.DoNotSave) && updates.Any())
            {
                // bulk save - should be the fastest way to do this
                callback?.Invoke($"Saving {updates.Count()} changes", 1, 1);
                serializer.Save(updates.Select(x => x.Value));
            }

            var folders = syncFileService.GetDirectories(folder);
            foreach (var children in folders)
            {
                actions.AddRange(ImportFolder(children, config, updates, force, callback));
            }

            if (actions.All(x => x.Success))
            {

                // LINQ 
                // actions.AddRange(cleanMarkers.Select(x => CleanFolder(x)).SelectMany(a => a));

                // only if there are no fails. 
                // then we consider the folder safe to clean 
                foreach (var cleanfile in cleanMarkers)
                {
                    actions.AddRange(CleanFolder(cleanfile, false, config.UseFlatStructure));
                }
                // remove the actual cleans (they will have been replaced by the deletes
                actions.RemoveAll(x => x.Change == ChangeType.Clean);

            }

            callback?.Invoke("", 1, 1);

            return actions;
        }

        /// <summary>
        ///  Get all the files in a folder and return them sorted by their level 
        /// </summary>
        private IList<LeveledFile> GetLevelOrderedFiles(string folder, IList<uSyncAction> actions)
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
                            Level = (node.GetLevel() * 1000) + node.GetItemSortOrder(), // will hopefully let us put things in sort order in one go. 
                            File = file
                        });

                        // debug.
                        logger.LogDebug("{file} {level}", file, (node.GetLevel() * 1000) + node.GetItemSortOrder());
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

        private class LeveledFile
        {
            public int Level { get; set; }
            public string File { get; set; }
        }

        private XElement LoadNode(string path)
        {
            syncFileService.EnsureFileExists(path);

            using (var stream = syncFileService.OpenRead(path))
            {
                return XElement.Load(stream);
            }
        }

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

    }
}
