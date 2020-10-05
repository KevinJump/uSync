using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ContentTemplateSerializer : ContentSerializer, ISyncOptionsSerializer<IContent>
    {
        private readonly IContentTypeService contentTypeService;

        public ContentTemplateSerializer(
            IEntityService entityService,
            ILocalizationService localizationService,
            IRelationService relationService,
            ILogger logger,
            IContentService contentService,
            IFileService fileService,
            IContentTypeService contentTypeService,
            SyncValueMapperCollection syncMappers)
            : base(entityService, localizationService, relationService, logger, contentService, fileService, syncMappers)
        {
            this.contentTypeService = contentTypeService;
        }

        protected override XElement SerializeInfo(IContent item, SyncSerializerOptions options)
        {
            var info = base.SerializeInfo(item, options);
            info.Add(new XElement("IsBlueprint", item.Blueprint));
            return info;
        }

        protected override SyncAttempt<IContent> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var attempt = FindOrCreate(node);
            if (!attempt.Success) throw attempt.Exception;

            var item = attempt.Result;

            var details = new List<uSyncChange>();

            var name = node.Name.LocalName;
            if (name != string.Empty)
            {
                details.AddUpdate("Name", item.Name, name);
                item.Name = name;
            }

            item.Blueprint = true;

            details.AddRange(DeserializeBase(item, node, options));

            // contentService.SaveBlueprint(item);

            return SyncAttempt<IContent>.Succeed(item.Name, item, ChangeType.Import, details);
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
            if (this.IsEmpty(node))
            {
                contentTypeAlias = node.GetAlias();
            }

            var contentType = contentTypeService.Get(contentTypeAlias);
            if (contentType != null)
            {
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

        protected override Attempt<IContent> CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var contentType = contentTypeService.Get(itemType);
            if (contentType == null) return
                    Attempt.Fail<IContent>(null, new ArgumentException($"Missing content Type {itemType}"));

            IContent item;
            if (parent != null)
            {
                item = new Content(alias, (IContent)parent, contentType);
            }
            else
            {
                item = new Content(alias, -1, contentType);
            }

            return Attempt.Succeed(item);
        }

        protected override Attempt<string> DoSaveOrPublish(IContent item, XElement node, SyncSerializerOptions options)
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
