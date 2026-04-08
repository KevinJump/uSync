using Microsoft.Extensions.Logging;

using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.Core.Documents;
using uSync.Core.Extensions;
using uSync.Core.Mapping;
using uSync.Core.Models;
using uSync.Core.Templates;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("C4E0E6F8-2742-4C7A-9244-321D5592987A", "contentTemplateSerializer", uSyncConstants.Serialization.Content)]
public class ContentTemplateSerializer : ContentSerializer, ISyncSerializer<IContent>
{
    private readonly IContentTypeService _contentTypeService;

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
        ISyncTemplateService templateService,
        ISyncDocumentUrlCleaner urlCleaner)
        : base(entityService, languageService, relationService, shortStringHelper, logger, contentService, syncMappers, userService, templateService, urlCleaner)
    {
        _contentTypeService = contentTypeService;
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
        if (!attempt.Success || attempt.Result is null) throw attempt.Exception ?? new Exception($"Unknown error {node.GetAlias()}");

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

        var contentTypeAlias = node.Name.LocalName;
        if (node.IsEmptyItem())
        {
            contentTypeAlias = node.GetAlias();
        }

        var contentType = _contentTypeService.Get(contentTypeAlias);
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
        });
    }

    protected override Task<SyncContentUpdateResult> DoSaveOrPublishAsync(IContent item, XElement node, SyncSerializerOptions options)
    {
        contentService.SaveBlueprint(item, null, options.UserId);
        var updatedItem = contentService.GetBlueprintById(item.Id);
        return Task.FromResult(new SyncContentUpdateResult
        {
            Success = true,
            Content = updatedItem ?? item,
            Message = "Blueprint saved"
        });
    }

    public override Task SaveItemAsync(IContent item)
        => uSyncTaskHelper.FromResultOf(() =>
        {
            contentService.SaveBlueprint(item, null, Constants.Security.SuperUserId);
        });

    public override Task DeleteItemAsync(IContent item)
        => uSyncTaskHelper.FromResultOf(() =>
        {
            contentService.DeleteBlueprint(item);
        });
}
