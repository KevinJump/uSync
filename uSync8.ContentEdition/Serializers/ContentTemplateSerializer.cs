using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.ContentEdition.Mapping;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.ContentEdition.Serializers
{
    [SyncSerializer("C4E0E6F8-2742-4C7A-9244-321D5592987A", "contentTemplateSerializer", uSyncConstants.Serialization.Content)]
    public class ContentTemplateSerializer : ContentSerializer, ISyncSerializer<IContent>
    {
        private readonly IContentTypeService contentTypeService;

        public ContentTemplateSerializer(
            IEntityService entityService, ILogger logger,
            IContentService contentService,
            IFileService fileService,
            IContentTypeService contentTypeService,
            SyncValueMapperCollection syncMappers)
            : base(entityService, logger, contentService,fileService, syncMappers)
        {
            this.contentTypeService = contentTypeService;
        }

        protected override XElement SerializeInfo(IContent item)
        {
            var info = base.SerializeInfo(item);
            info.Add(new XElement("IsBlueprint", item.Blueprint));
            return info;
        }

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

            item.Blueprint = true;

            DeserializeBase(item, node);

            // contentService.SaveBlueprint(item);

            return SyncAttempt<IContent>.Succeed(
                item.Name,
                item,
                ChangeType.Import,
                "");
        }

        public override IContent FindItem(XElement node)
        {
            var key = node.GetKey();
            if (key != Guid.Empty)
            {
                var item = FindItem(key);
                if (item != null) return item;
            }

            var contentTypeAlias = node.Name.LocalName;
            if (this.IsEmpty(node)) {
                contentTypeAlias = node.GetAlias();
            }

            var contentType = contentTypeService.Get(contentTypeAlias);
            if (contentType != null) {
                var blueprints = contentService.GetBlueprintsForContentTypes(contentType.Id);
                if (blueprints != null && blueprints.Any())
                {
                    return blueprints.FirstOrDefault(x => x.Name == node.GetAlias());
                }
            }

            return null;
         
        }

        protected override IContent FindItem(Guid key)
        {
            // TODO: Umbraco 8 bug, the key isn sometimes an old version
            var entity = entityService.Get(key);
            if (entity != null)
                return contentService.GetBlueprintById(entity.Id);

            return null;
        }

        protected override IContent FindItem(int id)
            => contentService.GetBlueprintById(id);

        protected override IContent CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var contentType = contentTypeService.Get(itemType);
            if (contentType == null) return null;

            if (parent != null)
            {
                return new Content(alias, (IContent)parent, contentType);
            }
            return new Content(alias, -1, contentType);
        }

        protected override Attempt<string> DoSaveOrPublish(IContent item, XElement node)
        {
            contentService.SaveBlueprint(item);
            return Attempt.Succeed<string>("blueprint saved");
        }

        protected override void SaveItem(IContent item)
            => contentService.SaveBlueprint(item);

        protected override void DeleteItem(IContent item)
            => contentService.DeleteBlueprint(item);
    }

}
