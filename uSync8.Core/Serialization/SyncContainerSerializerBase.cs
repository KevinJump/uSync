using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;

namespace uSync8.Core.Serialization
{
    public abstract class SyncContainerSerializerBase<TObject>
        : SyncTreeSerializerBase<TObject>
        where TObject : ITreeEntity
    {
        protected UmbracoObjectTypes containerType;

        public SyncContainerSerializerBase(IEntityService entityService, ILogger logger, UmbracoObjectTypes containerType) 
            : base(entityService, logger)
        {
            this.containerType = containerType;
        }

        protected override TObject FindOrCreate(XElement node)
        {
            TObject item = FindItem(node);
            if (item != null) return item;

            // create
            var parentId = -1;
            var parent = default(TObject);
            var treeItem = default(ITreeEntity);

            var info = node.Element("Info");

            var parentNode = info.Element("Parent");
            if (parentNode != null)
            {
                var parentKey = parentNode.Attribute("Key").ValueOrDefault(Guid.Empty);
                parent = FindItem(parentKey, parentNode.Value);
                if (parent != null)
                {
                    treeItem = parent;
                    parentId = parent.Id;
                }
            }

            if (parent == null)
            {
                // might be in a folder 
                var folder = info.Element("Folder");
                if (folder != null)
                {
                    var folderKey = folder.Attribute("Key").ValueOrDefault(Guid.Empty);
                    var container = FindFolder(folderKey, folder.Value);
                    if (container != null)
                    {
                        treeItem = container;
                    }
                }
            }

            var itemType = GetItemBaseType(node);

            var alias = node.GetAlias();

            return CreateItem(alias, parent != null ? parent : treeItem,itemType);
        }   

        private ITreeEntity TryCreateContainer(string name, ITreeEntity parent)
        {
            var children = entityService.GetChildren(parent.Id, containerType);
            if (children != null && children.Any(x => x.Name.InvariantEquals(name)))
            {
                return children.Single(x => x.Name.InvariantEquals(name));
            }

            // else create 
            var attempt = FindContainers(parent.Id, name);
            if (attempt)
                return (ITreeEntity)attempt.Result.Entity;

            return null;
        }


        #region Getters
        // Getters - get information we already know (either in the object or the XElement)

        protected XElement GetFolderNode(IEnumerable<EntityContainer> containers)
        {
            if (containers == null || !containers.Any())
                return null;

            var parentKey = containers.OrderBy(x => x.Level).LastOrDefault().Key.ToString();

            var folders = containers
                .OrderBy(x => x.Level)
                .Select(x => HttpUtility.UrlEncode(x.Name))
                .ToList();

            if (folders.Any())
            {
                var path = string.Join("/", folders);
                return new XElement("Folder", path);
                    //new XAttribute("Key", parentKey));
            }

            return null;

        }

        #endregion

        #region Finders

        protected abstract EntityContainer FindContainer(Guid key);
        protected abstract IEnumerable<EntityContainer> FindContainers(string folder, int level);
        protected abstract Attempt<OperationResult<OperationResultType, EntityContainer>> FindContainers(int parentId, string name);

        protected virtual ITreeEntity FindFolder(Guid key, string path)
        {
            var container = FindContainer(key);
            if (container != null) return container;

            /// else - we have to parse it like a path ... 
            var bits = path.Split('/');

            var rootFolder = HttpUtility.UrlDecode(bits[0]);

            var root = FindContainers(rootFolder, 1)
                .FirstOrDefault();
            if (root == null)
            {
                var attempt = FindContainers(-1, rootFolder);
                if (!attempt)
                {
                    return null;
                }

                root = attempt.Result.Entity;
            }

            if (root != null)
            {
                var current = (ITreeEntity)root;
                for (int i = 1; i < bits.Length; i++)
                {
                    var name = HttpUtility.UrlDecode(bits[i]);
                    current = TryCreateContainer(name, current);
                    if (current == null) break;
                }

                if (current != null)
                {
                    logger.Debug<TObject>("Folder Found");
                    if (current.Key != key)
                    {
                        logger.Debug<TObject>("Folder Found: Key Diffrent");
                        current.Key = key;
                        SaveContainer((EntityContainer)current);
                    }
                    return current;
                }
            }

            return null;
        }

        #endregion

        #region Container stuff
        protected abstract void SaveContainer(EntityContainer container);
        #endregion
    }
}
