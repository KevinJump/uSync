using Microsoft.Extensions.Logging;

using System.Security.Cryptography;
using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Extensions;
using uSync.Core.Mapping;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("B4060604-CF5A-46D6-8F00-257579A658E6", "MediaSerializer", uSyncConstants.Serialization.Media)]
public class MediaSerializer : ContentSerializerBase<IMedia>, ISyncSerializer<IMedia>
{
    private readonly IMediaService _mediaService;

    public MediaSerializer(
        IEntityService entityService,
        ILanguageService languageService,
        IRelationService relationService,
        IShortStringHelper shortStringHelper,
        ILogger<MediaSerializer> logger,
        IMediaService mediaService,
        SyncValueMapperCollection syncMappers)
        : base(entityService, languageService, relationService, shortStringHelper, logger, UmbracoObjectTypes.Media, syncMappers)
    {
        this._mediaService = mediaService;
        this.relationAlias = Constants.Conventions.RelationTypes.RelateParentMediaFolderOnDeleteAlias;

        // we don't serialize the media properties, 
        // you can't set them on an node in the Backoffice,
        // and they are auto calculated by umbraco anyway. 
        // & sometimes they just lead to false positives. 
        this.dontSerialize = [
            "umbracoWidth",
            "umbracoHeight",
            "umbracoBytes",
            "umbracoExtension"
        ];
    }

    protected override async Task<SyncAttempt<IMedia>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var attempt = await FindOrCreateAsync(node);
        if (!attempt.Success || attempt.Result is null)
            throw attempt.Exception ?? new Exception($"Unknown error {node.GetAlias()}");

        var item = attempt.Result;

        var details = new List<uSyncChange>();

        details.AddRange(await DeserializeBaseAsync(item, node, options));

        var propertyAttempt = await DeserializePropertiesAsync(item, node, options);
        if (!propertyAttempt.Success)
            return SyncAttempt<IMedia>.Fail(item.Name ?? item.Id.ToString(), item, ChangeType.Fail, "Failed to save properties",
                propertyAttempt.Exception ?? new Exception($"Error with properties {item.Id}"));

        var info = node.Element(uSyncConstants.Xml.Info);

        if (!options.GetSetting<bool>(uSyncConstants.DefaultSettings.IgnoreSortOrder, uSyncConstants.DefaultSettings.IgnoreSortOrder_Default))
        {
            var sortOrder = info?.Element(uSyncConstants.Xml.SortOrder).ValueOrDefault(-1) ?? -1;
            HandleSortOrder(item, sortOrder);
        }


        if (details.HasWarning() && options.FailOnWarnings())
        {
            // Fail on warning. means we don't save or publish because something is wrong ?
            return SyncAttempt<IMedia>.Fail(item.Name ?? item.Id.ToString(), item, ChangeType.ImportFail, "Failed with warnings", details,
                new Exception("Import failed because of warnings, and fail on warnings is true"));
        }

        var saveAttempt = _mediaService.Save(item);
        if (!saveAttempt.Success)
        {
            var errors = saveAttempt.Result?.EventMessages?.FormatMessages() ?? "";
            return SyncAttempt<IMedia>.Fail(item.Name ?? item.Id.ToString(), item, ChangeType.Fail, errors,
                saveAttempt.Exception ?? new Exception($"Error with item {item.Id}"));
        }

        // add warning messages if things are missing
        var message = "";
        if (details.Any(x => x.Change == ChangeDetailType.Warning))
            message += $" with warning(s)";

        // setting the saved flag on the attempt to true, stops base classes from saving the item.
        return SyncAttempt<IMedia>.Succeed(item.Name ?? item.Id.ToString(), item, ChangeType.Import, "", true, propertyAttempt.Result);
    }

    public override async Task<SyncAttempt<IMedia>> DeserializeSecondPassAsync(IMedia item, XElement node, SyncSerializerOptions options)
    {
        var details = await DeserializeSecondPassSharedAsync(item, node, options,
             Constants.Conventions.RelationTypes.RelateParentMediaFolderOnDeleteAlias);

        return SyncAttempt<IMedia>.Succeed(item.Name ?? item.Id.ToString(), item,
            details.Count == 0 ? ChangeType.NoChange : ChangeType.Import, details);
    }

    protected override void MoveToRecycleBin(IMedia item) => _mediaService.MoveToRecycleBin(item);
    protected override void SetTrashed(IMedia item) => ((ContentBase)item).Trashed = true;
    protected override void MoveItem(IMedia item, int parentId) => _mediaService.Move(item, parentId);
    protected override IMedia? GetByKey(Guid id) => _mediaService.GetById(id);

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(IMedia item, SyncSerializerOptions options)
    {
        var node = InitializeNode(item, item.ContentType.Alias, options);

        var info = await SerializeInfoAsync(item, options);
        var properties = await SerializePropertiesAsync(item, options);

        node.Add(info);
        node.Add(properties);

        // serializing the file hash, will mean if the image changes, then the media item will
        // trigger as a change - this doesn't mean the image will be updated other methods are
        // used to copy media between servers (uSync.Complete)
        if (options.GetSetting(uSyncConstants.DefaultSettings.IncludeFileHash, uSyncConstants.DefaultSettings.IncludeFileHash_Default))
            info.Add(SerializeFileHash(item));

        return SyncAttempt<XElement>.Succeed(
            item.Name ?? item.Id.ToString(),
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

    private static string GetFilePath(string? value)
    {
        if (value is null) return string.Empty;
        if (value.TryParseToJsonNode(out _) is false)
            return value;


        if (value.TryDeserialize<ImageCropperValue>(out var imageCrops) && imageCrops is not null)
        {
            return imageCrops.Src ?? string.Empty;
        }

        return value;
    }

    protected override Task<Attempt<IMedia?>> CreateItemAsync(string alias, ITreeEntity? parent, string itemType)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            try
            {
                var parentId = parent != null ? parent.Id : -1;
                var item = _mediaService.CreateMedia(alias, parentId, itemType);
                return Attempt.Succeed((IMedia)item);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating media item with alias {alias} and parent {parentId}", alias, parent?.Id);
                throw;
            }

        });
    }


    public override Task<IMedia?> FindItemAsync(Guid key)
        => uSyncTaskHelper.FromResultOf(() =>
        {
            return _mediaService.GetById(key);
        });

    protected override Task<IMedia?> FindAtRootAsync(string alias)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            var rootNodes = _mediaService.GetRootMedia();
            if (rootNodes.Any())
            {
                return rootNodes.FirstOrDefault(x => x.Name?.ToSafeAlias(shortStringHelper)?.InvariantEquals(alias) is true);
            }

            return null;
        });
    }

    public override Task SaveAsync(IEnumerable<IMedia> items)
        => uSyncTaskHelper.FromResultOf(() =>
        {
            var attempt = _mediaService.Save(items);
            if (attempt.Success is false)
            {
                throw new InvalidOperationException(
                    $"Could not save media items: {attempt.Result?.Result}");
            }
        });

    public override Task SaveItemAsync(IMedia item)
        => uSyncTaskHelper.FromResultOf(() =>
        {
            var attempt = _mediaService.Save(item);
            if (attempt.Success is false)
            {
                throw new InvalidOperationException(
                    $"Could not save media {item.Name}: {attempt.Result?.Result}");
            }
        });

    public override Task DeleteItemAsync(IMedia item)
        => uSyncTaskHelper.FromResultOf(() =>
        {
            var attempt = _mediaService.Delete(item);
            if (attempt.Success is false)
            {
                throw new InvalidOperationException(
                    $"Could not delete media {item.Name}: {attempt.Result?.Result}");
            }
        });

    protected override Task<IMedia?> FindParentByIdAsync(int id)
        => Task.FromResult(_mediaService.GetById(id));

}
