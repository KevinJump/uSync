using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using Umbraco.Core;

namespace uSync8.ContentEdition.Serializers
{
    [SyncSerializer("B4060604-CF5A-46D6-8F00-257579A658E6", "MediaSerializer", uSyncConstants.Serialization.Media)]
    public class MediaSerializer : ContentSerializerBase<IMedia>, ISyncSerializer<IMedia>
    {
        private readonly IMediaService mediaService;

        public MediaSerializer(
            IEntityService entityService, ILogger logger,
            IMediaService mediaService) 
            : base(entityService, logger, UmbracoObjectTypes.Media)
        {
            this.mediaService = mediaService;
        }

        protected override SyncAttempt<IMedia> DeserializeCore(XElement node)
        {
            var item = FindOrCreate(node);

            var name = node.Name.LocalName;
            if (name != string.Empty)
                item.Name = name;

            DeserializeBase(item, node);

            // mediaService.Save(item);

            return SyncAttempt<IMedia>.Succeed(
                item.Name, item, ChangeType.Import, "");
        }

        public override SyncAttempt<IMedia> DeserializeSecondPass(IMedia item, XElement node, SerializerFlags flags)
        {
            DeserializeProperties(item, node);

            var sortOrder = node.Element("Info").Element("SortOrder").ValueOrDefault(-1);
            if (sortOrder != -1)
            {
                item.SortOrder = sortOrder;
            }

            var attempt = mediaService.Save(item);
            if (attempt.Success)
                return SyncAttempt<IMedia>.Succeed(item.Name, ChangeType.Import);

            return SyncAttempt<IMedia>.Fail(item.Name, ChangeType.Fail, "");

        }

        protected override SyncAttempt<XElement> SerializeCore(IMedia item)
        {
            var node = InitializeNode(item, item.ContentType.Alias);

            var info = SerializeInfo(item);
            var properties = SerializeProperties(item);

            node.Add(info);
            node.Add(properties);

            return SyncAttempt<XElement>.Succeed(
                item.Name,
                node,
                typeof(IMedia),
                ChangeType.Export);
        }

        protected override IMedia CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var parentId = parent != null ? parent.Id : -1;
            var item = mediaService.CreateMedia(alias, parentId, itemType);
            return item;
        }

        protected override IMedia FindItem(int id)
            => mediaService.GetById(id);

        protected override IMedia FindItem(Guid key)
            => mediaService.GetById(key);

        protected override IMedia FindAtRoot(string alias)
        {
            var rootNodes = mediaService.GetRootMedia();
            if (rootNodes.Any())
            {
                return rootNodes.FirstOrDefault(x => x.Name.ToSafeAlias().InvariantEquals(alias));
            }

            return null;
        }

        public override void Save(IEnumerable<IMedia> items)
            => mediaService.Save(items);

        protected override void SaveItem(IMedia item)
            => mediaService.Save(item);
    }

}
