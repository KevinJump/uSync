using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("B3F7F247-6077-406D-8480-DB1004C8211C", "ContentTypeSerializer", uSyncConstants.Serialization.ContentType)]
    public class ContentTypeSerializer : ContentTypeBaseSerializer<IContentType>, ISyncSerializer<IContentType>
    {
        private readonly IContentTypeService contentTypeService;
        private readonly IFileService fileService;

        public ContentTypeSerializer(
            IEntityService entityService, ILogger logger,
            IDataTypeService dataTypeService,
            IContentTypeService contentTypeService,
            IFileService fileService)
            : base(entityService, logger, dataTypeService, contentTypeService, UmbracoObjectTypes.DocumentTypeContainer)
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
                info.Add(new XElement("Parent", parent.Alias,
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

        protected override void SerializeExtraProperties(XElement node, IContentType item, PropertyType property)
        {
            node.Add(new XElement("Variations", property.Variations));
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
            var item = FindOrCreate(node);

            DeserializeBase(item, node);

            DeserializeTabs(item, node);
            DeserializeProperties(item, node);

            // content type only property stuff.
            DeserializeContentTypeProperties(item, node);

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

        protected override void DeserializeExtraProperties(IContentType item, PropertyType property, XElement node)
        {
            property.Variations = node.Element("Variations").ValueOrDefault(ContentVariation.Nothing);
        }

        public override SyncAttempt<IContentType> DeserializeSecondPass(IContentType item, XElement node)
        {
            DeserializeCompositions(item, node);
            DeserializeStructure(item, node);
            contentTypeService.Save(item);

            CleanFolder(item, node);

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


        protected override IContentType CreateItem(string alias, IContentType parent, ITreeEntity treeItem, string itemType)
        {
            var item = new ContentType(-1)
            {
                Alias = alias
            };

            if (parent != null)
                item.AddContentType(parent);

            if (treeItem != null)
                item.SetParent(treeItem);

            

            return item;
        }

        protected override void SaveContainer(EntityContainer container)
        {
            logger.Debug<IContentType>("Saving Container (In main class) {0}", container.Key.ToString());
            contentTypeService.SaveContainer(container);
        }
    }
}
