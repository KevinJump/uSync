using Microsoft.Extensions.Logging;

using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.Core.Extensions;
using uSync.Core.Mapping;
using uSync.Core.Models;
using uSync.Core.Serialization.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("7EF3FF65-DD5E-4BDF-B82F-E8ED6F638BE9", "ElementSerializer", uSyncConstants.Serialization.Element)]
public class ElementSerializer : PublishableContentBaseSerializer<IElement>, ISyncSerializer<IElement>
{
    private readonly IElementService _elementService;
    private readonly IElementContainerService _containerService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IElementEditingService _elementEditingService;
    private readonly IIdKeyMap _keyMap;

    public ElementSerializer(
        IEntityService entityService,
        ILanguageService languageService,
        IRelationService relationService,
        IShortStringHelper shortStringHelper,
        ILogger<ContentSerializerBase<IElement>> logger,
        SyncValueMapperCollection syncMappers,
        IElementService elementService,
        IElementContainerService containerService,
        IContentTypeService contentTypeService,
        IUserService userService,
        IElementEditingService elementEditingService,
        IIdKeyMap keyMap)
        : base(entityService, languageService, relationService, shortStringHelper, logger,
            UmbracoObjectTypes.Element, syncMappers, userService)
    {
        _elementService = elementService;
        _containerService = containerService;
        _contentTypeService = contentTypeService;

        containerType = UmbracoObjectTypes.ElementContainer;
        _elementEditingService = elementEditingService;
        _keyMap = keyMap;
    }

    public override Task DeleteItemAsync(IElement item)
        => Task.FromResult(_elementService.Delete(item));

    public override Task<IElement?> FindItemAsync(Guid key)
        => Task.FromResult(_elementService.GetById(key));

    protected override void SetTrashed(IElement item)
        => ((ContentBase)item).Trashed = true;

    protected override void MoveToRecycleBin(IElement item)
    {
        _elementEditingService.MoveToRecycleBinAsync(item.Key, Constants.Security.SuperUserKey)
            .Wait();
    }

    protected override void MoveItem(IElement item, int parentId)
    {
            var parentAttempt = _keyMap.GetKeyForId(parentId, UmbracoObjectTypes.ElementContainer);
            Guid? parentKey = parentAttempt.Success ? parentAttempt.Result : null;
            _elementEditingService.RestoreAsync(item.Key, parentKey, Constants.Security.SuperUserKey)
                .Wait();
    }

    public override Task SaveItemAsync(IElement item)
    {
        _elementService.Save(item);
        return Task.CompletedTask;
    }

    // gone wrong if we get this. 
    protected override Task<Attempt<IElement?>> CreateItemAsync(string alias, ITreeEntity? parent, string itemType)
        => throw new NotImplementedException();

    protected override Task<Attempt<IElement?>> CreateItemAsync(ContentItemCreationOptions creation, SyncSerializerOptions options)
    {
        var contentType = _contentTypeService.Get(creation.ContentTypeAlias);
        if (contentType is null)
            return Task.FromResult(Attempt<IElement?>.Fail(new Exception($"No content type found with alias {creation.ContentTypeAlias}")));

        var element = new Element(creation.Alias, creation.Parent?.Id ?? -1, contentType);

        // elements require the node name is set before the save happens. 
        var changes = DeserializeName(element, creation.Node, options);

        var operationalResult = _elementService.Save(element);
        if (operationalResult.Success is false)
        {
            var messages = operationalResult.EventMessages?.FormatMessages() ?? "";
            return Task.FromResult(Attempt<IElement?>.Fail(new Exception($"Failed to create element with alias {creation.Alias} and content type {creation.ContentTypeAlias}. {messages}")));
        }

        return Task.FromResult(Attempt<IElement?>.Succeed(element));
    }

    // We have a mixture of IElement and IElementContainer, but the root finder is only looking for containers,
    // so we need to find a way to find elements at root level.
    protected override Task<IElement?> FindAtRootAsync(string alias)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            var rootElements = entityService.GetChildren(Constants.System.Root, UmbracoObjectTypes.Element);
            if (rootElements is null || rootElements.Any() is false) return null;

            var rootElement = rootElements.FirstOrDefault(x => x.Name?.Equals(alias, StringComparison.InvariantCultureIgnoreCase) is true);
            if (rootElement is null) return null;

            return GetByKey(rootElement.Key);

        });
    }

    protected override Task<SyncParentItem?> FindItemAsParent(Guid key)
    {
        return uSyncTaskHelper.FromResultOf<SyncParentItem?>(() =>
        {
            var container = entityService.Get(key, UmbracoObjectTypes.ElementContainer);
            if (container is null) return null;

            return container is null ? null :
                new SyncParentItem
                {
                    Id = container.Id,
                    Key = container.Key,
                    Name = container.Name ?? container.Id.ToString(),
                    Path = container.Path,
                    Level = container.Level
                };
        });
    }

    protected override async Task<SyncParentItem?> FindParentByIdAsync(int id)
    {
        var container = entityService.Get(id, UmbracoObjectTypes.ElementContainer);
        if (container is null) return null;

        var parent = await _containerService.GetAsync(container.Key);
        if (parent is not null)
        {
            return new SyncParentItem { Id = parent.Id, Key = parent.Key, Name = parent.Name ?? parent.Id.ToString() };
        }
        return null;
    }


    protected override Task<uSyncChange?> DeserializeTemplate(IElement item, XElement node)
        => Task.FromResult<uSyncChange?>(null);

    protected override ContentScheduleCollection GetScheduleById(int id)
        => _elementService.GetContentScheduleByContentId(id);

    public override Task SaveItemAsync(IElement item, int userId)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            try
            {
                _elementService.Save(item, userId);
            }
            catch (ArgumentNullException ex)
            {
                // we can get thrown a null argument exception by the notifier, 
                // which is non critical! but we are ignoring this error. ! <= 8.1.5
                if (!ex.Message.Contains("siteUri")) throw;
            }
        });
    }

    protected override PublishResult Unpublish(IElement item, string? culture, int userId = -1)
        => _elementService.Unpublish(item, culture, userId);

    protected override PublishResult Publish(IElement item, string[] cultures, int userId = -1)
        => _elementService.Publish(item, cultures, userId);

    protected override Task PersistSchedulesAsync(IElement item, ContentScheduleCollection schedules)
        => uSyncTaskHelper.FromResultOf(() => _elementService.PersistContentSchedule(item, schedules));

    public override Task<OperationResult> SaveWithSchedulesAsync(IElement item, int userId, ContentScheduleCollection scheduleCollection)
        => Task.FromResult(_elementService.Save(item, userId, scheduleCollection));
}
