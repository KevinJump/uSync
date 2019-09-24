using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.PropertyEditors.ValueConverters;
using Umbraco.Core.Services;

using uSync8.ContentEdition.Mapping;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.ContentEdition.Serializers
{
    [SyncSerializer("B4060604-CF5A-46D6-8F00-257579A658E6", "MediaSerializer", uSyncConstants.Serialization.Media)]
    public class MediaSerializer : ContentSerializerBase<IMedia>, ISyncSerializer<IMedia>
    {
        private readonly IMediaService mediaService;

        public MediaSerializer(
            IEntityService entityService, ILogger logger,
            IMediaService mediaService,
            SyncValueMapperCollection syncMappers)
            : base(entityService, logger, UmbracoObjectTypes.Media, syncMappers)
        {
            this.mediaService = mediaService;

            // we don't serialize the media properties, 
            // you can't set them on an node in the backoffice,
            // and they are auto calculated by umbraco anyway. 
            // & sometimes they just lead to false postives. 
            this.dontSerialize = new string[]
            {
                "umbracoWidth", "umbracoHeight", "umbracoBytes", "umbracoExtension"
            };
            
        }

        protected override SyncAttempt<IMedia> DeserializeCore(XElement node)
        {
            var item = FindOrCreate(node);

            DeserializeBase(item, node);

            // mediaService.Save(item);

            return SyncAttempt<IMedia>.Succeed(
                item.Name, item, ChangeType.Import, "");
        }

        public override SyncAttempt<IMedia> DeserializeSecondPass(IMedia item, XElement node, SerializerFlags flags)
        {
            DeserializeProperties(item, node);

            var info = node.Element("Info");

            var sortOrder = info.Element("SortOrder").ValueOrDefault(-1);
            HandleSortOrder(item, sortOrder);

            var trashed = info.Element("Trashed").ValueOrDefault(false);
            HandleTrashedState(item, trashed);

            var attempt = mediaService.Save(item);
            if (!attempt.Success) 
                return SyncAttempt<IMedia>.Fail(item.Name, ChangeType.Fail, "");

            // we return no-change so we don't trigger the second save 
            return SyncAttempt<IMedia>.Succeed(item.Name, ChangeType.NoChange );
        }

        protected override void HandleTrashedState(IMedia item, bool trashed)
        {
            if (!trashed && item.Trashed)
            {
                // if the item is trashed, then moving it back to the parent value 
                // restores it.
                mediaService.Move(item, item.ParentId);
            }
            else if (trashed && !item.Trashed)
            {
                // move to the recycle bin
                mediaService.MoveToRecycleBin(item);
            }
        }


        protected override SyncAttempt<XElement> SerializeCore(IMedia item)
        {
            var node = InitializeNode(item, item.ContentType.Alias);

            var info = SerializeInfo(item);
            var properties = SerializeProperties(item);

            node.Add(info);
            node.Add(properties);

            info.Add(SerializeFileHash(item));

            return SyncAttempt<XElement>.Succeed(
                item.Name,
                node,
                typeof(IMedia),
                ChangeType.Export);
        }

        private XElement SerializeFileHash(IMedia item)
        {
            if (item.HasProperty("umbracoFile"))
            {
                var value = item.GetValue<string>("umbracoFile");
                var path = GetFilePath(value);

                if (!string.IsNullOrWhiteSpace(path))
                {
                    using (var stream = mediaService.GetMediaFileContentStream(path))
                    {
                        if (stream != null)
                        {
                            using (MD5 md5 = MD5.Create())
                            {
                                stream.Seek(0, SeekOrigin.Begin);
                                var hash = md5.ComputeHash(stream);

                                return new XElement("FileHash", hash);
                            }
                        }
                    }
                }
            }

            return new XElement("Hash", "");
            
        }

        private string GetFilePath(string value)
        {
            if (value.DetectIsJson())
            {
                // image cropper.
                var imageCrops = JsonConvert.DeserializeObject<ImageCropperValue>(value, new JsonSerializerSettings
                {
                    Culture = CultureInfo.InvariantCulture,
                    FloatParseHandling = FloatParseHandling.Decimal
                });

                return imageCrops.Src;
            }

            return value;
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
        {
            // TODO: Umbraco 8 bug, find by key sometimes returns an old version
            var entity = entityService.Get(key);
            if (entity != null)
                return mediaService.GetById(entity.Id);

            return null;
        }

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

        protected override void DeleteItem(IMedia item)
            => mediaService.Delete(item);

    }

}
