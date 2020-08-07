using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

using uSync8.Core.Models;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("B3073706-5037-4FBD-A015-DF38D61F2934", "MediaTypeSerializer", uSyncConstants.Serialization.MediaType)]
    public class MediaTypeSerializer : ContentTypeBaseSerializer<IMediaType>, ISyncOptionsSerializer<IMediaType>
    {
        private readonly IMediaTypeService mediaTypeService;

        public MediaTypeSerializer(
            IEntityService entityService, ILogger logger,
            IDataTypeService dataTypeService,
            IMediaTypeService mediaTypeService) 
            : base(entityService, logger, dataTypeService, mediaTypeService, UmbracoObjectTypes.MediaTypeContainer)
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
                var folderNode = GetFolderNode(mediaTypeService.GetContainers(item));
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

            details.AddRange(DeserializeProperties(item, node));

            CleanTabs(item, node);

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
            var item = new MediaType(-1)
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
