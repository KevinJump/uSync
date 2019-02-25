using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
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
    public abstract class SyncHandlerTreeBase<TObject, TService> : SyncHandlerBase<TObject, TService>
        where TObject : ITreeEntity
        where TService : IService
    {
        protected SyncHandlerTreeBase(
            IEntityService entityService, 
            IProfilingLogger logger, 
            ISyncSerializer<TObject> serializer,
            ISyncTracker<TObject> tracker,
            SyncFileService syncFileService) 
            : base(entityService, logger, serializer, tracker, syncFileService)
        {
        }

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

            foreach(var file in files)
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
                catch(XmlException ex)
                {
                    // one of the files is wrong. (do we stop or carry on)
                    logger.Warn<TObject>($"Error loading file: {file} [{ex.Message}]");
                    actions.Add(uSyncActionHelper<TObject>.SetAction(
                        SyncAttempt<TObject>.Fail(Path.GetFileName(file), ChangeType.Fail, $"Failed to Load: {ex.Message}"), file, false)); 
                }
            }

            // loaded - now process.

            var count = 0;
            foreach(var node in nodes.OrderBy(x => x.Level))
            {
                count++;
                callback?.Invoke($"{Path.GetFileName(node.File)}", count, nodes.Count);

                var attempt = Import(node.File, config, force);
                if (attempt.Success && attempt.Item != null)
                {
                    updates.Add(node.File, attempt.Item);
                }

                actions.Add(uSyncActionHelper<TObject>.SetAction(attempt, node.File, IsTwoPass));
            }

            var folders = syncFileService.GetDirectories(folder);
            foreach (var children in folders)
            {
                actions.AddRange(ImportFolder(children, config, updates, force, callback));
            }

            callback?.Invoke("", 1,1);

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

            using(var stream = syncFileService.OpenRead(path))
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

        protected override string GetItemName(TObject item) => item.Name;
    }
    
}
