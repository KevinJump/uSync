using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.BackOffice.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Serialization;

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
            SyncFileService syncFileService, 
            uSyncBackOfficeSettings settings) 
            : base(entityService, logger, serializer, syncFileService, settings)
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
        protected override IEnumerable<uSyncAction> ImportFolder(string folder, bool force, Dictionary<string, TObject> updates)
        {
            // if not using flat, then directory structure is doing
            // this for us. 
            if (globalSettings.UseFlatStructure == false)
                return base.ImportFolder(folder, force, updates);

            List<uSyncAction> actions = new List<uSyncAction>();

            var files = syncFileService.GetFiles(folder, "*.config");

            List<LeveledFile> nodes = new List<LeveledFile>();

            foreach(var file in files)
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

            // loaded - now process.

            foreach(var node in nodes.OrderBy(x => x.Level))
            {
                var attempt = Import(node.File, force);
                if (attempt.Success && attempt.Item != null)
                {
                    updates.Add(node.File, attempt.Item);
                }

                actions.Add(uSyncActionHelper<TObject>.SetAction(attempt, node.File, IsTwoPass));
            }

            var folders = syncFileService.GetDirectories(folder);
            foreach (var children in folders)
            {
                actions.AddRange(ImportFolder(children, force, updates));
            }


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
        virtual protected string GetItemFileName(IUmbracoEntity item)
        {
            if (item != null)
            {
                if (globalSettings.UseFlatStructure)
                    return item.Key.ToString();

                return item.Name.ToSafeFileName();
            }

            return Guid.NewGuid().ToString();
        }

        override protected string GetItemPath(TObject item)
        {
            if (globalSettings.UseFlatStructure)
                return GetItemFileName((IUmbracoEntity)item);

            return GetEntityPath((IUmbracoEntity)item);
        }

        protected string GetEntityPath(IUmbracoEntity item)
        {
            var path = string.Empty;
            if (item != null)
            {
                if (item.ParentId > 0)
                {
                    var parent = entityService.Get(item.ParentId);
                    if (parent != null)
                    {
                        path = GetEntityPath(parent);
                    }
                }

                path = Path.Combine(path, GetItemFileName(item));
            }

            return path;
        }
    }
}
