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
    [SyncSerializer("C4E0E6F8-2742-4C7A-9244-321D5592987A", "contentTemplateSerializer", uSyncConstants.Serialization.Content)]
    public class ContentTemplateSerializer : ContentSerializer, ISyncSerializer<IContent>
    {
        private readonly IContentTypeService contentTypeService;

        public ContentTemplateSerializer(
            IEntityService entityService,
            IContentService contentService,
            IContentTypeService contentTypeService)
            : base(entityService, contentService)
        {
            this.contentTypeService = contentTypeService;
        }

        protected override XElement SerializeInfo(IContent item)
        {
            var info = base.SerializeInfo(item);
            info.Add(new XElement("IsBlueprint", item.Blueprint));
            return info;
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
            => contentService.GetBlueprintById(key);

        protected override IContent FindItem(int id)
            => contentService.GetBlueprintById(id);

        protected override IContent CreateItem(string alias, IContent parent, ITreeEntity treeItem, string itemType)
        {
            var contentType = contentTypeService.Get(ItemType);
            if (contentType == null) return null;

            var item = new Content(alias, parent, contentType);
            return item;
        }

        protected override Attempt<string> DoSaveOrPublish(IContent item, XElement node)
        {
            contentService.SaveBlueprint(item);
            return Attempt.Succeed<string>("blueprint saved");
        }
    }
}
