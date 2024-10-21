using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("B3073706-5037-4FBD-A015-DF38D61F2934", "MediaTypeSerializer", uSyncConstants.Serialization.MediaType)]
public class MediaTypeSerializer : ContentTypeBaseSerializer<IMediaType>, ISyncSerializer<IMediaType>
{
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IMediaTypeContainerService _mediaTypeContainerService;

    public MediaTypeSerializer(
        IEntityService entityService, ILogger<MediaTypeSerializer> logger,
        IDataTypeService dataTypeService,
        IMediaTypeService mediaTypeService,
        IShortStringHelper shortStringHelper,
        AppCaches appCaches, IMediaTypeContainerService mediaTypeContainerService)
        : base(entityService, mediaTypeContainerService, logger, dataTypeService, mediaTypeService, UmbracoObjectTypes.MediaTypeContainer, shortStringHelper, appCaches)
    {
        this._mediaTypeService = mediaTypeService;
        _mediaTypeContainerService = mediaTypeContainerService;
    }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(IMediaType item, SyncSerializerOptions options)
    {
        var node = SerializeBase(item);
        var info = SerializeInfo(item);

        var parent = item.ContentTypeComposition.FirstOrDefault(x => x.Id == item.ParentId);
        if (parent != null)
        {
            info.Add(new XElement(uSyncConstants.Xml.Parent, parent.Alias,
                new XAttribute(uSyncConstants.Xml.Key, parent.Key)));
        }
        else if (item.Level != 1)
        {
            // in a folder
            var folderNode = await GetFolderNodeAsync(item); //TODO: Cache this call.
            if (folderNode != null)
                info.Add(folderNode);
        }

        info.Add(SerializeCompositions((ContentTypeCompositionBase)item));

        node.Add(info);
        node.Add(SerializeProperties(item));
        node.Add(SerializeStructure(item));
        node.Add(SerializeTabs(item));

        return SyncAttempt<XElement>.Succeed(item.Name ?? item.Alias, node, typeof(IMediaType), ChangeType.Export);
    }

    protected override async Task<SyncAttempt<IMediaType>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        if (!IsValid(node))
            throw new ArgumentException("Invalid XML Format");

        var details = new List<uSyncChange>();

        var attempt = await FindOrCreateAsync(node);
        if (!attempt.Success || attempt.Result is null)
            throw attempt.Exception ?? new Exception($"Unknown error {node.GetAlias()}");

        var item = attempt.Result;

        details.AddRange(await DeserializeBaseAsync(item, node));
        details.AddRange(DeserializeTabs(item, node));
        details.AddRange(DeserializeProperties(item, node, options));

        details.AddRange(await DeserializeCompositionsAsync(item, node));

        CleanTabs(item, node, options);

        return DeserializedResult(item, details, options);
    }

    public override async Task<SyncAttempt<IMediaType>> DeserializeSecondPassAsync(IMediaType item, XElement node, SyncSerializerOptions options)
    {
        var details = new List<uSyncChange>();

        details.AddRange(await DeserializeCompositionsAsync(item, node));
        details.AddRange(await DeserializeStructureAsync(item, node));

        SetSafeAliasValue(item, node, false);

        CleanTabAliases(item);
        CleanTabs(item, node, options);

        bool saveInSerializer = !options.Flags.HasFlag(SerializerFlags.DoNotSave);
        if (saveInSerializer && item.IsDirty())
            await this.SaveItemAsync(item);

        return SyncAttempt<IMediaType>.Succeed(item.Name ?? item.Alias, item, ChangeType.Import, "", saveInSerializer, details);
    }

    protected override async Task<Attempt<IMediaType?>> CreateItemAsync(string alias, ITreeEntity? parent, string itemType)
    {
        var safeAlias = GetSafeItemAlias(alias);

        var item = new MediaType(shortStringHelper, -1)
        {
            Alias = safeAlias
        };

        if (parent != null)
        {
            if (parent is IMediaType mediaParent)
                item.AddContentType(mediaParent);

            item.SetParent(parent);
        }

        AddAlias(safeAlias);

        return Attempt.Succeed(item as IMediaType);
    }


    protected override async Task<Attempt<EntityContainer?, EntityContainerOperationStatus>> CreateContainerAsync(Guid parentKey, string name)
    {
        var parent = await _mediaTypeContainerService.GetAsync(parentKey);
        if (parent is null) return Attempt<EntityContainer?, EntityContainerOperationStatus>.Fail(EntityContainerOperationStatus.ParentNotFound);
        if (parent.Name is null) return Attempt<EntityContainer?, EntityContainerOperationStatus>.Fail(EntityContainerOperationStatus.ParentNotFound);

        var existing = (await _mediaTypeContainerService.GetAsync(parent.Name, parent.Level)).FirstOrDefault(x => x.Name.InvariantEquals(name));
        if (existing is null)
        {
            return await _mediaTypeContainerService.CreateAsync(Guid.NewGuid(), name, parentKey, Constants.Security.SuperUserKey);
        }
        else
        {
            return await _mediaTypeContainerService.UpdateAsync(existing.Key, name, Constants.Security.SuperUserKey);
        }
    }
}
