using Microsoft.Extensions.Logging;

using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Documents;
using uSync.Core.Extensions;
using uSync.Core.Mapping;
using uSync.Core.Models;
using uSync.Core.Serialization.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("C4E0E6F8-2742-4C7A-9244-321D5592987A", "contentTemplateSerializer", uSyncConstants.Serialization.Content)]
public class ContentTemplateSerializer : ContentSerializer, ISyncSerializer<IContent>
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentBlueprintContainerService _containerService;

    public ContentTemplateSerializer(
        IEntityService entityService,
        ILanguageService languageService,
        IRelationService relationService,
        IShortStringHelper shortStringHelper,
        ILogger<ContentTemplateSerializer> logger,
        IContentService contentService,
        IContentTypeService contentTypeService,
        SyncValueMapperCollection syncMappers,
        IUserService userService,
        ITemplateService templateService,
        ISyncDocumentUrlCleaner urlCleaner,
        IContentBlueprintContainerService containerService)
        : base(entityService, languageService, relationService, shortStringHelper, logger, contentService, syncMappers, userService, templateService, urlCleaner)
    {
        _contentTypeService = contentTypeService;
        _containerService = containerService;
        this.umbracoObjectType = UmbracoObjectTypes.DocumentBlueprint;
    }

    protected override async Task<XElement> SerializeInfoAsync(IContent item, SyncSerializerOptions options)
    {
        var info = await base.SerializeInfoAsync(item, options);
        info.Add(new XElement("IsBlueprint", item.Blueprint));
        return info;
    }

    protected override async Task<SyncAttempt<IContent>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var attempt = await FindOrCreateAsync(node);
        if (!attempt.Success || attempt.Result is null) 
            throw attempt.Exception ?? new Exception($"Unknown error {node.GetAlias()}");

        var item = attempt.Result;

        var details = new List<uSyncChange>();

        var name = node.Name.LocalName;
        if (name != string.Empty)
        {
            details.AddUpdate("Name", item.Name ?? item.Id.ToString(), name);
            item.Name = name;
        }

        item.Blueprint = true;

        details.AddRange(await DeserializeBaseAsync(item, node, options));

        var propertiesAttempt = await DeserializePropertiesAsync(item, node, options);
        if (!propertiesAttempt.Success)
        {
            return SyncAttempt<IContent>.Fail(item.Name ?? item.Id.ToString(), item, ChangeType.ImportFail, "Failed to deserialized properties", attempt.Exception);
        }

        details.AddRange(propertiesAttempt.Result);


        // contentService.SaveBlueprint(item);

        return SyncAttempt<IContent>.Succeed(item.Name ?? item.Id.ToString(), item, ChangeType.Import, details);
    }

    public override async Task<IContent?> FindItemAsync(XElement node)
    {
        var key = node.GetKey();
        if (key != Guid.Empty)
        {
            var item = await FindItemAsync(key);
            if (item != null) return item;
        }

        var contentTypeAlias = node.IsEmptyItem()
            ? node.GetAlias()
            : node.Name.LocalName;

        var contentType = _contentTypeService.Get(contentTypeAlias);
        if (contentType is null) return null;

        var blueprints = contentService.GetBlueprintsForContentTypes(contentType.Id);
        if (blueprints is null || blueprints.Any() == false) return null;
        
        return blueprints
            .FirstOrDefault(x => x.Name == node.GetAlias());

    }

    public override Task<IContent?> FindItemAsync(Guid key)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            // TODO: Umbraco 8 bug, the key is sometimes an old version
            var entity = entityService.Get(key);
            if (entity != null)

                return contentService.GetBlueprintById(entity.Id);

            return null;
        });
    }

    protected override Task<Attempt<IContent?>> CreateItemAsync(string alias, ITreeEntity? parent, string itemType)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            var contentType = _contentTypeService.Get(itemType);
            if (contentType == null) return
                    Attempt.Fail<IContent?>(null, new ArgumentException($"Missing content Type {itemType}"));

            // parent can be either an existing blueprint (unlikely) or the
            // DocumentBlueprintContainer (folder) the blueprint lives in, so
            // we create by id rather than assuming it's always an IContent.
            var item = new Content(alias, parent?.Id ?? -1, contentType);

            return Attempt.Succeed<IContent?>(item);
        });
    }

    protected override async Task<Attempt<IContent?>> FindOrCreateAsync(XElement node)
    {
        var item = await FindItemAsync(node);
        if (item is not null) return Attempt.Succeed(item);

        var info = node.Element(uSyncConstants.Xml.Info);
        var alias = node.GetAlias();

        var parentNode = info?.Element(uSyncConstants.Xml.Parent);
        var parentKey = parentNode?.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty) ?? Guid.Empty;

        ITreeEntity? parent = null;

        if (parentKey != Guid.Empty)
        {
            item = await FindItemAsync(alias, parentKey);
            if (item is not null) return Attempt.Succeed(item);

            // the parent might be another blueprint, or the
            // DocumentBlueprintContainer (folder) the blueprint lives in.
            parent = await FindItemAsync(parentKey);
            parent ??= await FindContainerAsync(parentKey);
            parent ??= await FindOrCreateContainerAsync(parentKey, parentNode?.Value ?? string.Empty,
                info?.Element(uSyncConstants.Xml.Path).ValueOrDefault(string.Empty) ?? string.Empty);
        }

        var contentTypeAlias = info?.Element("ContentType").ValueOrDefault(node.Name.LocalName) ?? node.Name.LocalName;

        return await CreateItemAsync(alias, parent, contentTypeAlias);
    }


    // a blueprint's parent can be the DocumentBlueprintContainer (folder) it
    // lives in rather than another blueprint, so include containers when we
    // resolve the parent for path/level calculations.
    protected override async Task<SyncParentItem?> FindItemAsParent(Guid key)
    {
        var parent = await base.FindItemAsParent(key);
        if (parent is not null) return parent;

        var container = await FindContainerAsync(key);
        return container is null ? null : ToParentItem(container);
    }

    private async Task<EntityContainer?> FindContainerAsync(Guid key)
        => await _containerService.GetAsync(key);

    protected override async Task<SyncParentItem?> CreateParentIfMissingAsync(XElement parentNode, string path)
    {
        var key = parentNode.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);
        var name = parentNode.ValueOrDefault(string.Empty);
        if (key == Guid.Empty || string.IsNullOrEmpty(name)) return null;

        var container = await FindOrCreateContainerAsync(key, name, path);
        return container is null ? null : ToParentItem(container);
    }

    private static SyncParentItem ToParentItem(EntityContainer container)
        => new()
        {
            Id = container.Id,
            Key = container.Key,
            Name = container.Name ?? container.Id.ToString(),
            Path = container.Path,
            Level = container.Level
        };
   
    /// <summary>
    ///  find (or create) the DocumentBlueprintContainer folder chain for a blueprint,
    ///  the same way missing folders get created on the way in for content types / data types.
    /// </summary>
    private async Task<EntityContainer?> FindOrCreateContainerAsync(Guid key, string name, string friendlyPath)
    {
        var container = await FindContainerAsync(key);
        if (container is not null) return container;

        if (string.IsNullOrWhiteSpace(name)) return null;

        // the friendly path is '/folder/folder/blueprintName' - the folder chain
        // is everything except the blueprint's own name at the end.
        var folderNames = friendlyPath.ToDelimitedList("/").ToList();
        if (folderNames.Count > 0) folderNames.RemoveAt(folderNames.Count - 1);
        if (folderNames.Count == 0) folderNames.Add(name);

        EntityContainer? parent = null;

        for (var index = 0; index < folderNames.Count; index++)
        {
            var folderName = folderNames[index];
            var isTargetFolder = index == folderNames.Count - 1;

            var existing = (await _containerService.GetAsync(folderName, index + 1))
                .FirstOrDefault(x => x.Name.InvariantEquals(folderName) &&
                    (parent == null || x.ParentId == parent.Id));

            if (existing is not null)
            {
                parent = existing;
                continue;
            }

            var containerKey = isTargetFolder ? key : Guid.NewGuid();
            var attempt = await _containerService.CreateAsync(containerKey, folderName, parent?.Key, Constants.Security.SuperUserKey);
            if (!attempt.Success || attempt.Result is null) return parent;

            parent = attempt.Result;
        }

        return parent;
    }

    protected override Task<SyncContentUpdateResult<IContent>> DoSaveOrPublishAsync(IContent item, XElement node, SyncSerializerOptions options)
    {
        contentService.SaveBlueprint(item, null, options.UserId);
        var updatedItem = contentService.GetBlueprintById(item.Id);
        return Task.FromResult(new SyncContentUpdateResult<IContent>
        {
            Success = true,
            Content = updatedItem ?? item,
            Message = "Blueprint saved"
        });
    }

    public override Task SaveItemAsync(IContent item)
    {
        contentService.SaveBlueprint(item, null);
        return Task.CompletedTask;
    }

    public override Task DeleteItemAsync(IContent item)
    {
        contentService.DeleteBlueprint(item);
        return Task.CompletedTask;
    }
}
