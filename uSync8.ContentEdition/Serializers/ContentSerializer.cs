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
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.ContentEdition.Serializers
{
    [SyncSerializer("5CB57139-8AF7-4813-95AD-C075D74636C2", "ContentSerializer", uSyncConstants.Serialization.Content)]
    public class ContentSerializer : SyncTreeSerializerBase<IContent>
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
            var node = new XElement(item.ContentType.Alias,
                    new XAttribute("Key", item.Key),
                    new XAttribute("Alias", item.Name),
                    new XAttribute("Level", item.Level));



            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IContent), ChangeType.Export);
        }

        protected override SyncAttempt<IContent> DeserializeCore(XElement node)
        {

            // TODO: We will need to override (or extend) the find and create (we need to pass the type)
            var item = FindOrCreate(node);

            return SyncAttempt<IContent>.Succeed(
                item.Name,
                item,
                ChangeType.Import, "");
        }

        protected override IContent CreateItem(string alias, IContent parent, ITreeEntity treeItem, string itemType)
        {
            IContent item;
            if (parent != null)
                item = contentService.Create(alias, parent.Key, itemType);
            else
                item = contentService.Create(alias, -1, itemType);

            return item;
        }


        #region Container Stuff (we might not use this?)
        protected override EntityContainer GetContainer(Guid key)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<EntityContainer> GetContainers(string folder, int level)
        {
            throw new NotImplementedException();
        }

        protected override Attempt<OperationResult<OperationResultType, EntityContainer>> CreateContainer(int parentId, string name)
        {
            throw new NotImplementedException();
        }
        #endregion

        protected override IContent GetItem(Guid key)
            => contentService.GetById(key);

        protected override IContent GetItem(string alias)
        {
            throw new NotImplementedException();
        }

    }
}
