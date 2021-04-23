using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using uSync.Core.Mapping;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("C4E0E6F8-2742-4C7A-9244-321D5592987A", "contentTemplateSerializer", uSyncConstants.Serialization.Content)]
    public class ContentTemplateSerializer : ContentSerializer, ISyncSerializer<IContent>
    {
        private readonly IContentTypeService contentTypeService;

        public ContentTemplateSerializer(
            IEntityService entityService,
            ILocalizationService localizationService,
            IRelationService relationService,
            IShortStringHelper shortStringHelper,
            ILogger<ContentTemplateSerializer> logger,
            IContentService contentService,
            IFileService fileService,
            IContentTypeService contentTypeService,
            SyncValueMapperCollection syncMappers)
            : base(entityService, localizationService, relationService, shortStringHelper, logger, contentService, fileService, syncMappers)
        {
            this.contentTypeService = contentTypeService;
            this.umbracoObjectType = UmbracoObjectTypes.DocumentBlueprint;
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

        public override IContent FindItem(Guid key)
        {
            // TODO: Umbraco 8 bug, the key isn sometimes an old version
            var entity = entityService.Get(key);
            if (entity != null)
                return contentService.GetBlueprintById(entity.Id);

            return null;
        }

        // public override string GetItemPath(IContent item) => base.GetItemPath(item) + "/" + item.Name.ToSafeAlias(); 

        public override IContent FindItem(int id)
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

        public override void SaveItem(IContent item)
            => contentService.SaveBlueprint(item);

        public override void DeleteItem(IContent item)
            => contentService.DeleteBlueprint(item);
    }

}
