using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization
{
    public abstract class SyncTreeSerializerBase<TObject> : SyncSerializerBase<TObject>
        where TObject : ITreeEntity
    {
        protected SyncTreeSerializerBase(IEntityService entityService)
            : base(entityService)
        {
        }

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
                return new XElement("Folder", path,
                    new XAttribute("Key", parentKey));
            }

            return null;

        }


        protected TObject FindOrCreate(XElement node, string typeElement = "")
        {
            TObject item = default(TObject);
            var key = node.GetKey();
            if (key == Guid.Empty) return default(TObject);

            item = GetItem(key);
            if (item != null) return item;

            var alias = node.GetAlias();
            if (alias == string.Empty) return default(TObject);
            item = GetItem(alias);
            if (item != null) return item;

            // create
            var parentId = -1;
            var parent = default(TObject);
            var treeItem = default(ITreeEntity);

            var info = node.Element("Info");
            var master = info.Element("Master");
            if (master != null)
            {
                var parentKey = master.Attribute("Key").ValueOrDefault(Guid.Empty);
                parent = GetItem(parentKey, master.Value);

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
                    var container = LookupFolderByKeyOrPath(folderKey, folder.Value);
                    if (container != null)
                    {
                        treeItem = container;
                    }
                }
            }

            var itemType = string.Empty;
            if (!string.IsNullOrWhiteSpace(typeElement))
            {
                itemType = info.Element(typeElement).ValueOrDefault(string.Empty);
            }

            return CreateItem(alias, parent, treeItem, string.Empty);
        }

        protected abstract TObject CreateItem(string alias, TObject parent, ITreeEntity treeItem, string itemType);

        protected virtual ITreeEntity LookupFolderByKeyOrPath(Guid key, string path)
        {
            var container = GetContainer(key);
            if (container != null) return container;

            /// else - we have to parse it like a path ... 
            var bits = path.Split('/');

            var rootFolder = HttpUtility.UrlDecode(bits[0]);

            var root = GetContainers(rootFolder, 1)
                .FirstOrDefault();
            if (root == null)
            {
                var attempt = CreateContainer(-1, rootFolder);
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
                    return current;
            }

            return null;
        }

        private ITreeEntity TryCreateContainer(string name, ITreeEntity parent)
        {
            var children = entityService.GetChildren(parent.Id, UmbracoObjectTypes.DocumentTypeContainer);
            if (children != null && children.Any(x => x.Name.InvariantEquals(name)))
            {
                return children.Single(x => x.Name.InvariantEquals(name));
            }

            // else create 
            var attempt = CreateContainer(parent.Id, name);
            if (attempt)
                return (ITreeEntity)attempt.Result.Entity;

            return null;
        }

        protected abstract EntityContainer GetContainer(Guid key);
        protected abstract IEnumerable<EntityContainer> GetContainers(string folder, int level);
        protected abstract Attempt<OperationResult<OperationResultType, EntityContainer>> CreateContainer(int parentId, string name);
    }
}
