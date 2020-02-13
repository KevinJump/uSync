using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.BackOffice.SyncHandlers
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

        protected SyncHandlerLevelBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, appCaches, syncFileService)
        { }

        protected SyncHandlerLevelBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            AppCaches appCaches,
            ISyncDependencyChecker<TObject> checker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, appCaches, checker, syncFileService)
        { }

        /// <summary>
        ///  this is the simple interface, based purely on level, 
        ///  we could get clever (like dependency trees for content types)
        ///  
        ///  but that would have to be implimented lower down (and it doesn't 
        ///  really matter for things in containers only things that parent others).
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="force"></param>
        /// <param name="updates"></param>
        /// <returns></returns>
        protected override IEnumerable<uSyncAction> ImportFolder(string folder, HandlerSettings config, Dictionary<string, TObject> updates, bool force, SyncUpdateCallback callback)
        {

            // if not using flat, then directory structure is doing
            // this for us. 
            if (config.UseFlatStructure == false)
                return base.ImportFolder(folder, config, updates, force, callback);

            List<uSyncAction> actions = new List<uSyncAction>();

            var files = syncFileService.GetFiles(folder, "*.config");

            List<LeveledFile> nodes = new List<LeveledFile>();

            callback?.Invoke("Calculating import order", 0, 1);
            logger.Debug(handlerType, "Calculating import order");

            foreach (var file in files)
            {
                try
                {
                    var node = LoadNode(file);
                    if (node != null)
                    {
                        nodes.Add(new LeveledFile
                        {
                            Level = node.GetLevel(),
                            File = file
                        });
                    }
                }
                catch (XmlException ex)
                {
                    // one of the files is wrong. (do we stop or carry on)
                    logger.Warn(handlerType, $"Error loading file: {file} [{ex.Message}]");
                    actions.Add(uSyncActionHelper<TObject>.SetAction(
                        SyncAttempt<TObject>.Fail(Path.GetFileName(file), ChangeType.Fail, $"Failed to Load: {ex.Message}"), file, false));
                }
            }

            // loaded - now process.
            var flags = SerializerFlags.None;
            if (force) flags |= SerializerFlags.Force;
            if (config.BatchSave) flags |= SerializerFlags.DoNotSave;

            var cleanMarkers = new List<string>();

            foreach (var item in nodes.OrderBy(x => x.Level).Select((Node, Index) => new { Node, Index }))
            {
                callback?.Invoke($"{Path.GetFileNameWithoutExtension(item.Node.File)}", item.Index, nodes.Count);

                logger.Debug(handlerType, "{Index} Importing: {File}, [Level {Level}]", item.Index, item.Node.File, item.Node.Level);

                var attempt = Import(item.Node.File, config, flags);
                if (attempt.Success)
                {
                    if (attempt.Change == ChangeType.Clean)
                    {
                        cleanMarkers.Add(item.Node.File);
                    }
                    else if (attempt.Item != null)
                    {
                        updates.Add(item.Node.File, attempt.Item);
                    }
                }

                actions.Add(uSyncActionHelper<TObject>.SetAction(attempt, item.Node.File, IsTwoPass));
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


        // path helpers
        virtual protected string GetItemFileName(IUmbracoEntity item, bool useGuid)
        {
            if (item != null)
            {
                if (useGuid)
                    return item.Key.ToString();

                return item.Name.ToSafeFileName();
            }

            return Guid.NewGuid().ToString();
        }

        override protected string GetItemPath(TObject item, bool useGuid, bool isFlat)
        {
            if (isFlat)
                return GetItemFileName((IUmbracoEntity)item, useGuid);

            return GetEntityPath((IUmbracoEntity)item, useGuid, true);
        }

        protected string GetEntityPath(IUmbracoEntity item, bool useGuid, bool isTop)
        {
            var path = string.Empty;
            if (item != null)
            {
                if (item.ParentId > 0)
                {
                    var parent = entityService.Get(item.ParentId);
                    if (parent != null)
                    {
                        path = GetEntityPath(parent, useGuid, false);
                    }
                }

                // we only want the guid file name at the top of the tree 
                path = Path.Combine(path, GetItemFileName(item, useGuid && isTop));
            }

            return path;
        }
    }
}
