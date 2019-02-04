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
            : base(entityService, UmbracoObjectTypes.Document)
        {
            this.contentService = contentService;
        }

        #region Serialization

        protected override SyncAttempt<XElement> SerializeCore(IContent item)
        {
            var node = InitializeNode(item, item.ContentType.Alias);

            var info = SerializeInfo(item);
            var properties = SerializeProperties(item);

            node.Add(info);
            node.Add(properties);

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IContent), ChangeType.Export);
        }

        protected override XElement SerializeInfo(IContent item)
        {
            var info = base.SerializeInfo(item);

            info.Add(new XElement("Published", item.Published));

            var published = new XElement("PublishedCultures");
            foreach(var culture in item.PublishedCultures)
            {
                published.Add(new XElement("Culture", culture));
            }

            var cultures = new XElement("Cultures");
            foreach(var culture in item.AvailableCultures)
            {
                cultures.Add(new XElement("Culture", culture));
            }

            info.Add(new XElement("SortOrder", item.SortOrder));
            
            return info;
        }

        #endregion

        #region Deserialization

        protected override SyncAttempt<IContent> DeserializeCore(XElement node)
        {
            var item = FindOrCreate(node);

            if (item.Trashed)
            {
                // TODO: Where has changed trashed state gone?
            }

            var name = node.Name.LocalName;
            if (name != string.Empty)
                item.Name = name;

            DeserializeBase(item, node);

            contentService.Save(item);

            return SyncAttempt<IContent>.Succeed(
                item.Name,
                item,
                ChangeType.Import,
                "");
        }


        public override SyncAttempt<IContent> DeserializeSecondPass(IContent item, XElement node)
        {
            DeserializeProperties(item, node);

            // sort order
            var sortOrder = node.Element("Info").Element("SortOrder").ValueOrDefault(-1);
            if (sortOrder != -1)
            {
                item.SortOrder = sortOrder;
            }

            // published status
            // this does the last save and publish
            if (DeserializePublishedStatuses(item, node))
            {
                return SyncAttempt<IContent>.Succeed(item.Name, ChangeType.Import);
            }

            return SyncAttempt<IContent>.Fail(item.Name, ChangeType.ImportFail, "");
            // second pass, is when we do the publish and stuff.
        }

        private Attempt<string> DeserializePublishedStatuses(IContent item, XElement node)
        {
            var info = node.Element("Info");


            contentService.SaveAndPublish(item);

            return Attempt.Succeed("Done");

        }

        #endregion

        protected override IContent CreateItem(string alias, IContent parent, ITreeEntity treeItem, string itemType)
        {
            var parentId = parent != null ? parent.Id : -1;
            var item = contentService.Create(alias, parentId, itemType);
            return item; 
        }

        #region Finders

        protected override IContent FindItem(int id)
            => contentService.GetById(id);

        protected override IContent FindItem(Guid key)
            => contentService.GetById(key);

        #endregion


    }
}
