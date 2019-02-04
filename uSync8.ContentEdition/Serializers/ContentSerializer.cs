using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.ContentEdition.Serializers
{
    [SyncSerializer("5CB57139-8AF7-4813-95AD-C075D74636C2", "ContentSerializer", uSyncConstants.Serialization.Content)]
    public class ContentSerializer : ContentSerializerBase<IContent>, ISyncSerializer<IContent>
    {
        private readonly IContentService contentService;

        public ContentSerializer(
            IEntityService entityService,
            IContentService contentService)
            : base(entityService)
        {
            this.contentService = contentService;
        }

        protected override SyncAttempt<XElement> SerializeCore(IContent item)
        {
            var node = InitializeNode(item, item.ContentType.Alias);

            node.Add(new XAttribute("Published", item.Published));

            foreach(var property in item.Properties.OrderBy(x => x.Alias))
            {
                var propertyNode = new XElement(property.Alias);

                foreach(var value in property.Values)
                {
                    var valueNode = new XElement("Value",
                        new XAttribute("Culture", value.Culture ?? string.Empty),
                        new XAttribute("Segment", value.Segment ?? string.Empty));

                    valueNode.Value = value.EditedValue.ToString();

                    propertyNode.Add(valueNode);
                }

                node.Add(propertyNode);
            }

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IContent), ChangeType.Export);
        }

        protected override SyncAttempt<IContent> DeserializeCore(XElement node)
        {
            var item = FindOrCreate(node);

            if (item.Trashed)
            {
                // TODO: Where has changed trashed state gone?
            }

            var name = node.Attribute("alias").ValueOrDefault(string.Empty);
            if (name != string.Empty)
                item.Name = name;

            var parentId = -1;
            var parentKey = node.Attribute("parent").ValueOrDefault(Guid.Empty);
            if (parentKey != Guid.Empty) {
                var parent = GetItem(parentKey);
                parentId = parent.Id;
            }

            if (item.ParentId != parentId)
                item.ParentId = parentId;



            return SyncAttempt<IContent>.Succeed(
                item.Name,
                item,
                ChangeType.Import,
                "");
        }

        protected override IContent CreateItem(string alias, IContent parent, ITreeEntity treeItem, string itemType)
        {
            var parentId = parent != null ? parent.Id : -1;
            var item = contentService.Create(alias, parentId, itemType);
            return item; 
        }

        // /////////////

        protected override IContent GetItem(int id)
            => contentService.GetById(id);

        protected override IContent GetItem(Guid key)
            => contentService.GetById(key);

        protected override IContent GetItem(string alias, Guid parent)
        {
            var parentItem = contentService.GetById(parent);
            if (parentItem != null)
            {
                var children = entityService.GetChildren(parentItem.Id, UmbracoObjectTypes.Document);
                var child = children.FirstOrDefault(x => x.Name.InvariantEquals(alias));
                if (child != null)
                    return contentService.GetById(child.Id);
            }

            return null;

        }



    }
}
