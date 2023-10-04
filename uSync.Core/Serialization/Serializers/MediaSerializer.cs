using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Mapping;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("B4060604-CF5A-46D6-8F00-257579A658E6", "MediaSerializer", uSyncConstants.Serialization.Media)]
    public class MediaSerializer : ContentSerializerBase<IMedia>, ISyncSerializer<IMedia>
    {
        private readonly IMediaService _mediaService;

        public MediaSerializer(
            IEntityService entityService,
            ILocalizationService localizationService,
            IRelationService relationService,
            IShortStringHelper shortStringHelper,
            ILogger<MediaSerializer> logger,
            IMediaService mediaService,
            SyncValueMapperCollection syncMappers)
            : base(entityService, localizationService, relationService, shortStringHelper, logger, UmbracoObjectTypes.Media, syncMappers)
        {
            this._mediaService = mediaService;
            this.relationAlias = Constants.Conventions.RelationTypes.RelateParentMediaFolderOnDeleteAlias;

            // we don't serialize the media properties, 
            // you can't set them on an node in the backoffice,
            // and they are auto calculated by umbraco anyway. 
            // & sometimes they just lead to false postives. 
            this.dontSerialize = new string[]
            {
                "umbracoWidth", "umbracoHeight", "umbracoBytes", "umbracoExtension"
            };
        }

        protected override SyncAttempt<IMedia> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var attempt = FindOrCreate(node);
            if (!attempt.Success) throw attempt.Exception;

            var item = attempt.Result;

            var details = new List<uSyncChange>();

            details.AddRange(DeserializeBase(item, node, options));

            if (node.Element("Info") != null)
            {
                var trashed = node.Element("Info").Element("Trashed").ValueOrDefault(false);
                var restoreParent = node.Element("Info").Element("Trashed").Attribute("Parent").ValueOrDefault(Guid.Empty);
                details.AddNotNull(HandleTrashedState(item, trashed, restoreParent));
            }

            var propertyAttempt = DeserializeProperties(item, node, options);
            if (!propertyAttempt.Success)
                return SyncAttempt<IMedia>.Fail(item.Name, item, ChangeType.Fail, "Failed to save properties", propertyAttempt.Exception);

            var info = node.Element("Info");

            if (!options.GetSetting<bool>("IgnoreSortOrder", false))
            {
                var sortOrder = info.Element("SortOrder").ValueOrDefault(-1);
                HandleSortOrder(item, sortOrder);
            }


            if (details.HasWarning() && options.FailOnWarnings())
            {
                // Fail on warning. means we don't save or publish because something is wrong ?
                return SyncAttempt<IMedia>.Fail(item.Name, item, ChangeType.ImportFail, "Failed with warnings", details,
                    new Exception("Import failed because of warnings, and fail on warnings is true"));
            }

            var saveAttempt = _mediaService.Save(item);
            if (!saveAttempt.Success) {
                var errors = saveAttempt.Result?.EventMessages?.FormatMessages() ?? "";
                return SyncAttempt<IMedia>.Fail(item.Name, item, ChangeType.Fail, errors, saveAttempt.Exception);
            }

            // add warning messages if things are missing
            var message = "";
            if (details.Any(x => x.Change == ChangeDetailType.Warning))
                message += $" with warning(s)";

            // setting the saved flag on the attempt to true, stops base classes from saving the item.
            return SyncAttempt<IMedia>.Succeed(item.Name, item, ChangeType.Import, "", true, propertyAttempt.Result);
        }

        protected override uSyncChange HandleTrashedState(IMedia item, bool trashed, Guid restoreParentKey)
        {
            if (!trashed && item.Trashed)
            {
                // if the item is trashed, then moving it back to the parent value 
                // restores it.

                var restoreParentId = GetRelationParentId(item, restoreParentKey, Constants.Conventions.RelationTypes.RelateParentDocumentOnDeleteAlias);
                _mediaService.Move(item, restoreParentId);

                CleanRelations(item, "relateParentMediaFolderOnDelete");

                return uSyncChange.Update("Restored", item.Name, "Recycle Bin", item.ParentId.ToString());
            }
            else if (trashed && !item.Trashed)
            {
                // clean any rouge relations 
                CleanRelations(item, "relateParentMediaFolderOnDelete");

                // move to the recycle bin
                _mediaService.MoveToRecycleBin(item);
                return uSyncChange.Update("Moved to Bin", item.Name, "", "Recycle Bin");
            }

            return null;
        }


        protected override SyncAttempt<XElement> SerializeCore(IMedia item, SyncSerializerOptions options)
        {
            var node = InitializeNode(item, item.ContentType.Alias, options);

            var info = SerializeInfo(item, options);
            var properties = SerializeProperties(item, options);

            node.Add(info);
            node.Add(properties);

            // serializing the file hash, will mean if the image changes, then the media item will
            // trigger as a change - this doesn't mean the image will be updated other methods are
            // used to copy media between servers (uSync.Complete)
            if (options.GetSetting("IncludeFileHash", false))
                info.Add(SerializeFileHash(item));

            return SyncAttempt<XElement>.Succeed(
                item.Name,
                node,
                typeof(IMedia),
                ChangeType.Export);
        }

        private XElement SerializeFileHash(IMedia item)
        {
            try
            {
                if (item.HasProperty("umbracoFile"))
                {
                    var value = item.GetValue<string>("umbracoFile");
                    var path = GetFilePath(value);

                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        using (var stream = _mediaService.GetMediaFileContentStream(path))
                        {
                            if (stream != null)
                            {
                                using (HashAlgorithm hashAlgorithm = CryptoConfig.AllowOnlyFipsAlgorithms ? SHA1.Create() : MD5.Create())
                                {
                                    stream.Seek(0, SeekOrigin.Begin);
                                    var hash = hashAlgorithm.ComputeHash(stream);

                                    return new XElement("FileHash", hash);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // can happen when the media locations get moved.
                logger.LogError(ex, "Error reading media file: {item}", item.Name);
            }

            return new XElement("FileHash", "");

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

        protected override Attempt<IMedia> CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var parentId = parent != null ? parent.Id : -1;
            var item = _mediaService.CreateMedia(alias, parentId, itemType);
            return Attempt.Succeed((IMedia)item);
        }

        public override IMedia FindItem(int id)
        {
            var item = _mediaService.GetById(id);
            if (item != null)
            {
                AddToNameCache(id, item.Key, item.Name);
                return item;
            }
            return null;
        }


        public override IMedia FindItem(Guid key)
            => _mediaService.GetById(key);

        protected override IMedia FindAtRoot(string alias)
        {
            var rootNodes = _mediaService.GetRootMedia();
            if (rootNodes.Any())
            {
                return rootNodes.FirstOrDefault(x => x.Name.ToSafeAlias(shortStringHelper).InvariantEquals(alias));
            }

            return null;
        }

        public override void Save(IEnumerable<IMedia> items)
            => _mediaService.Save(items);

        public override void SaveItem(IMedia item)
            => _mediaService.Save(item);

        public override void DeleteItem(IMedia item)
            => _mediaService.Delete(item);

    }

}
