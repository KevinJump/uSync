using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("B3073706-5037-4FBD-A015-DF38D61F2934", "MediaTypeSerializer", uSyncConstants.Serialization.MediaType)]
    public class MediaTypeSerializer : ContentTypeBaseSerializer<IMediaType>, ISyncSerializer<IMediaType>
    {
        private readonly IMediaTypeService mediaTypeService;

        public MediaTypeSerializer(
            IUmbracoVersion umbracoVersion,
            IEntityService entityService, ILogger<MediaTypeSerializer> logger,
            IDataTypeService dataTypeService,
            IMediaTypeService mediaTypeService,
            IShortStringHelper shortStringHelper)
            : base(umbracoVersion, entityService, logger, dataTypeService, mediaTypeService, UmbracoObjectTypes.MediaTypeContainer, shortStringHelper)
        {
            this.mediaTypeService = mediaTypeService;
        }

        protected override SyncAttempt<XElement> SerializeCore(IMediaType item, SyncSerializerOptions options)
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
                // in a folder
                var folderNode = GetFolderNode(item); //TODO: Cache this call.
                if (folderNode != null)
                    info.Add(folderNode);
            }

            info.Add(SerializeCompostions((ContentTypeCompositionBase)item));

            node.Add(info);
            node.Add(SerializeProperties(item));
            node.Add(SerializeStructure(item));
            node.Add(SerializeTabs(item));

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IMediaType), ChangeType.Export);
        }

        protected override SyncAttempt<IMediaType> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            if (!IsValid(node))
                throw new ArgumentException("Invalid XML Format");

            var details = new List<uSyncChange>();

            var attempt = FindOrCreate(node);
            if (!attempt.Success) throw attempt.Exception;

            var item = attempt.Result;

            details.AddRange(DeserializeBase(item, node));
            details.AddRange(DeserializeTabs(item, node));

            // mediaTypeService.Save(item);

            details.AddRange(DeserializeProperties(item, node, options));

            CleanTabs(item, node, options);

            return SyncAttempt<IMediaType>.Succeed(item.Name, item, ChangeType.Import, details);
        }

        public override SyncAttempt<IMediaType> DeserializeSecondPass(IMediaType item, XElement node, SyncSerializerOptions options)
        {
            var details = new List<uSyncChange>();

            details.AddRange(DeserializeCompositions(item, node));
            details.AddRange(DeserializeStructure(item, node));

            if (!options.Flags.HasFlag(SerializerFlags.DoNotSave) && item.IsDirty())
                mediaTypeService.Save(item);

            return SyncAttempt<IMediaType>.Succeed(item.Name, item, ChangeType.Import, details);
        }

        protected override Attempt<IMediaType> CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var item = new MediaType(shortStringHelper, -1)
            {
                Alias = alias
            };

            if (parent != null)
            {
                if (parent is IMediaType mediaParent)
                    item.AddContentType(mediaParent);

                item.SetParent(parent);
            }

            return Attempt.Succeed((IMediaType)item);
        }
    }
}
