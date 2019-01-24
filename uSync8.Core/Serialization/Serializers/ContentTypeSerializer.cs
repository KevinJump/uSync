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

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializerAttribute("B3F7F247-6077-406D-8480-DB1004C8211C", "ContentTypeSerializer", uSyncConstants.Serialization.ContentType)]
    public class ContentTypeSerializer : ContentTypeBaseSerializer<IContentType>, ISyncSerializer<IContentType>
    {
        private readonly IContentTypeService contentTypeService;
        private readonly IFileService fileService;

        public ContentTypeSerializer(
            IEntityService entityService,
            IDataTypeService dataTypeService,
            IContentTypeService contentTypeService,
            IFileService fileService)
            : base(entityService, dataTypeService)
        {
            this.contentTypeService = contentTypeService;
            this.fileService = fileService;
        }

        protected override SyncAttempt<XElement> SerializeCore(IContentType item)
        {
            var node = SerializeBase(item);

            var info = SerializeInfo(item);

            var parent = item.ContentTypeComposition.FirstOrDefault(x => x.Id == item.ParentId);
            if (parent != null)
            {
                info.Add(new XElement("Master", parent.Alias,
                            new XAttribute("Key", parent.Key)));
            }
            else if (item.Level != 1)
            {
                // in a folder. 
                var folders = contentTypeService.GetContainers(item)
                    .OrderBy(x => x.Level)
                    .Select(x => HttpUtility.UrlEncode(x.Name))
                    .ToList();

                if (folders.Any())
                {
                    string path = string.Join("/", folders);
                    info.Add(new XElement("Folder", path));
                }
            }

            // compositions ? 
            // (might be in the ContentTypeCore, because you can also do this
            //  with media?)
            var compNode = new XElement("Compositions");
            var compositions = item.ContentTypeComposition;
            foreach (var composition in compositions.OrderBy(x => x.Key))
            {
                compNode.Add(new XElement("Composition", composition.Alias,
                    new XAttribute("Key", composition.Key)));
            }
            info.Add(compNode);

            // templates
            var templateAlias =
                (item.DefaultTemplate != null && item.DefaultTemplate.Id != 0)
                ? item.DefaultTemplate.Alias
                : "";

            info.Add(new XElement("DefaultTemplate", templateAlias));

            var templates = SerailizeTemplates(item);
            if (templates != null)
                info.Add(templates);

            node.Add(info);
            node.Add(SerializeStructure(item));
            node.Add(SerializeProperties(item));
            node.Add(SerializeTabs(item));

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IContentType), ChangeType.Export);
        }

        private XElement SerailizeTemplates(IContentType item)
        {
            var node = new XElement("AllowedTemplates");
            if (item.AllowedTemplates.Any())
            {
                foreach (var template in item.AllowedTemplates.OrderBy(x => x.Alias))
                {
                    node.Add(new XElement("Template", template.Alias,
                        new XAttribute("Key", template.Key)));
                }
            }

            return node;
        }

        protected override SyncAttempt<IContentType> DeserializeCore(XElement node)
        {
            if (node.Name.LocalName != ItemType)
                return SyncAttempt<IContentType>.Fail(node.Name.LocalName, ChangeType.Fail);

            if (!IsValid(node))
                throw new ArgumentException("Invalid XML Format");

            var item = FindOrCreate(node);

            DeserializeBase(item, node);
            DeserializeTabs(item, node);
            DeserializeProperties(item, node);

            // content type only property stuff.
            DeserializeContentTypeProperties(item, node);


            DeserializeStructure(item, node);

            // clean tabs 
            CleanTabs(item, node);

            // templates 
            DeserializeTemplates(item, node);


            contentTypeService.Save(item);

            return SyncAttempt<IContentType>.Succeed(
                item.Name,
                item,
                ChangeType.Import,
                "");
        }

        private void DeserializeContentTypeProperties(IContentType item, XElement node)
        {
            if (node == null) return;
            var info = node.Element("Info");
            if (info == null) return;

            item.IsContainer = info.Element("IsListView").ValueOrDefault(false);

            var masterTemplate = info.Element("DefaultTemplate").ValueOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(masterTemplate))
            {
                var template = fileService.GetTemplate(masterTemplate);
                if (template != null)
                    item.SetDefaultTemplate(template);
            }
        }

        private void DeserializeTemplates(IContentType item, XElement node)
        {
            var templates = node.Element("Info").Element("AllowedTemplates");
            if (templates == null) return;

            var allowedTemplates = new List<ITemplate>();

            foreach (var template in templates.Elements("Template"))
            {
                var alias = template.Value;
                var key = template.Attribute("Key").ValueOrDefault(Guid.Empty);

                var templateItem = default(ITemplate);

                if (key != Guid.Empty)
                    templateItem = fileService.GetTemplate(key);

                if (templateItem == null)
                    templateItem = fileService.GetTemplate(alias);

                if (templateItem != null)
                {
                    allowedTemplates.Add(templateItem);
                }
            }

            item.AllowedTemplates = allowedTemplates;
        }


        private IContentType FindOrCreate(XElement node)
        {
            var info = node.Element("Info");
            IContentType item = null;
            var key = info.Element("Key").ValueOrDefault(Guid.Empty);
            if (key == Guid.Empty) return null;

            item = LookupByKey(key);
            if (item != null) return item;


            var alias = info.Element("Alias").ValueOrDefault(string.Empty);
            if (alias == string.Empty) return null;
            item = LookupByAlias(alias);
            if (item != null) return item;

            // create
            var parentId = -1;
            var parent = default(IContentType);
            var treeItem = default(ITreeEntity);

            var master = info.Element("Master");
            if (master != null)
            {
                var parentKey = master.Attribute("Key").ValueOrDefault(Guid.Empty);
                parent = LookupByKeyOrAlias(parentKey, master.Value);

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

            item = new ContentType(-1)
            {
                Alias = alias
            };

            if (parent != null)
                item.AddContentType(parent);

            item.SetParent(treeItem);

            return item;
        }

        private ITreeEntity LookupFolderByKeyOrPath(Guid key, string path)
        {
            var container = contentTypeService.GetContainer(key);
            if (container != null) return container;

            /// else - we have to parse it like a path ... 
            var bits = path.Split('/');

            var rootFolder = HttpUtility.UrlDecode(bits[0]);

            var root = contentTypeService.GetContainers(rootFolder, 1)
                .FirstOrDefault();
            if (root == null)
            {
                var attempt = contentTypeService.CreateContainer(-1, rootFolder);
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
            var attempt = contentTypeService.CreateContainer(parent.Id, name);
            if (attempt)
                return (ITreeEntity)attempt.Result.Entity;

            return null;
        }

        protected override IContentType LookupById(int id)
            => contentTypeService.Get(id);

        protected override IContentType LookupByKey(Guid key)
            => contentTypeService.Get(key);


        protected override IContentType LookupByAlias(string alias)
            => contentTypeService.Get(alias);

        /// <summary>
        ///  does the property with the alias we want exist on
        ///  any of the ContentTypes that may inherit this one?
        /// </summary>
        protected override bool PropertyExistsOnComposite(IContentTypeBase item, string alias)
        {
            var allTypes = contentTypeService.GetAll().ToList();

            var allProperties = allTypes
                    .Where(x => x.ContentTypeComposition.Any(y => y.Id == item.Id))
                    .Select(x => x.PropertyTypes)
                    .ToList();

            return allProperties.Any(x => x.Any(y => y.Alias == alias));
        }
    }
}
