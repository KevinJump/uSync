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
using uSync.Core.Extensions;

namespace uSync.Core.Serialization
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

        protected override Attempt<TObject> FindOrCreate(XElement node)
        {

            TObject item = FindItem(node);
            if (item != null) return Attempt.Succeed(item);

            logger.Debug(serializerType, "FindOrCreate: Creating");

            // create
            var parentId = -1;
            var parent = default(TObject);
            var treeItem = default(ITreeEntity);

            var info = node.Element("Info");

            var parentNode = info.Element("Parent");
            if (parentNode != null)
            {
                logger.Debug(serializerType, "Finding Parent");

                var parentKey = parentNode.Attribute("Key").ValueOrDefault(Guid.Empty);
                parent = FindItem(parentKey, parentNode.Value);
                if (parent != null)
                {
                    logger.Debug(serializerType, "Parent Found {0}", parent.Id);
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

                    logger.Debug(serializerType, "Searching for Parent by folder {0}", folderKey);

                    var container = FindFolder(folderKey, folder.Value);
                    if (container != null)
                    {
                        treeItem = container;
                        logger.Debug(serializerType, "Parent is Folder {0}", treeItem.Id);

                        // update the container key if its different (because we don't serialize folders on their own)
                        if (container.Key != folderKey)
                        {
                            if (container.Key != folderKey)
                            {
                                logger.Debug(serializerType, "Folder Found: Key Different");
                                container.Key = folderKey;
                                SaveContainer(container);
                            }
                        }
                    }
                }
            }

            var itemType = GetItemBaseType(node);

            var alias = node.GetAlias();

            return CreateItem(alias, parent != null ? parent : treeItem,itemType);
        }   

        private EntityContainer TryCreateContainer(string name, ITreeEntity parent)
        {
            var children = entityService.GetChildren(parent.Id, containerType);
            if (children != null && children.Any(x => x.Name.InvariantEquals(name)))
            {
                var item = children.Single(x => x.Name.InvariantEquals(name));
                return FindContainer(item.Key);
            }

            // else create 
            var attempt = FindContainers(parent.Id, name);
            if (attempt)
                return attempt.Result.Entity;

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
                    // new XAttribute("Key", parentKey));
            }

            return null;

        }

        #endregion

        #region Finders

        protected abstract EntityContainer FindContainer(Guid key);
        protected abstract IEnumerable<EntityContainer> FindContainers(string folder, int level);
        protected abstract Attempt<OperationResult<OperationResultType, EntityContainer>> FindContainers(int parentId, string name);

        protected virtual EntityContainer FindFolder(Guid key, string path)
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
                var current = root;
                for (int i = 1; i < bits.Length; i++)
                {
                    var name = HttpUtility.UrlDecode(bits[i]);
                    current = TryCreateContainer(name, current);
                    if (current == null) break;
                }

                if (current != null)
                {
                    logger.Debug(serializerType, "Folder Found {0}", current.Name);
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
