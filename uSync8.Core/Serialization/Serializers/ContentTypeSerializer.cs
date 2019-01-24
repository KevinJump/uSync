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
    [SyncSerializer("B3F7F247-6077-406D-8480-DB1004C8211C", "ContentTypeSerializer", uSyncConstants.Serialization.ContentType)]
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
                var folderNode = this.GetFolderNode(contentTypeService.GetContainers(item));
                if (folderNode != null)
                    info.Add(folderNode);
            }

            // compositions ? 
            info.Add(SerializeCompostions((ContentTypeCompositionBase)item));

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
            if (!IsValid(node))
                throw new ArgumentException("Invalid XML Format");

            var item = FindOrCreate(node);

            DeserializeBase(item, node);
            DeserializeTabs(item, node);
            DeserializeProperties(item, node);

            // content type only property stuff.
            DeserializeContentTypeProperties(item, node);


            // 2nd pass (don't need to do it twice)
            // DeserializeStructure(item, node);

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

        public override SyncAttempt<IContentType> DesrtializeSecondPass(IContentType item, XElement node)
        {
            DeserializeCompositions(item, node);
            DeserializeStructure(item, node);
            contentTypeService.Save(item);

            return SyncAttempt<IContentType>.Succeed(item.Name, item, ChangeType.Import);
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

        private void DeserializeCompositions(IContentType item, XElement node)
        {
            var comps = node.Element("Info").Element("Compositions");
            if (comps == null) return;
            List<IContentTypeComposition> compositions = new List<IContentTypeComposition>();

            foreach (var compositionNode in comps.Elements("Composition"))
            {
                var alias = compositionNode.Value;
                var key = compositionNode.Attribute("Key").ValueOrDefault(Guid.Empty);

                var type = LookupByKeyOrAlias(key, alias);
                if (type != null)
                    compositions.Add(type);
            }

            item.ContentTypeComposition = compositions;
        }


        protected override IContentType CreateItem(string alias, IContentType parent, ITreeEntity treeItem)
        {
            var item = new ContentType(-1)
            {
                Alias = alias
            };

            if (parent != null)
                item.AddContentType(parent);

            item.SetParent(treeItem);

            return item;
        }

        // TODO: Workout what base class service we should pass to
        //       not need all these little overrides here

        protected override IContentType LookupById(int id)
            => contentTypeService.Get(id);

        protected override IContentType LookupByKey(Guid key)
            => contentTypeService.Get(key);

        protected override IContentType LookupByAlias(string alias)
            => contentTypeService.Get(alias);

        protected override Attempt<OperationResult<OperationResultType, EntityContainer>> CreateContainer(int parentId, string name)
            => contentTypeService.CreateContainer(parentId, name);

        protected override EntityContainer GetContainer(Guid key)
            => contentTypeService.GetContainer(key);
                       
        protected override IEnumerable<EntityContainer> GetContainers(string folder, int level)
            => contentTypeService.GetContainers(folder, level);

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
