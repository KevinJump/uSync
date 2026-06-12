using Microsoft.Extensions.Logging;

using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;

using uSync.Core.Models;

using static Umbraco.Cms.Core.Constants.HttpContext;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("8AD4ED7F-9E47-4918-ACAE-146132F5AB56",
    "Element Serializer", uSyncConstants.Serialization.ElementContainer, IsTwoPass = true)]
internal class ElementContainerSerializer : SyncSerializerBase<EntityContainer>,
    ISyncEntityContainerSerializer<EntityContainer>
{
    private readonly IElementContainerService _containerService;
    private readonly IIdKeyMap _idKeyMap;

    public ElementContainerSerializer(
        IEntityService entityService,
        ILogger<SyncSerializerBase<EntityContainer>> logger,
        IElementContainerService containerService,
        IIdKeyMap idKeyMap) : base(entityService, logger)
    {
        _containerService = containerService;
        _idKeyMap = idKeyMap;
    }

    public UmbracoObjectTypes ContainedType => UmbracoObjectTypes.Element;

    public override async Task DeleteItemAsync(EntityContainer item)
        => await _containerService.DeleteAsync(item.Key, Constants.Security.SuperUserKey);

    public override async Task<EntityContainer?> FindItemAsync(Guid key)
        => await _containerService.GetAsync(key);

    public override Task<EntityContainer?> FindItemAsync(string alias)
        => Task.FromResult<EntityContainer?>(null);

    public string GetEntityContainerPath(EntityContainer item) => string.Empty;

    public override string ItemAlias(EntityContainer item) => item.Name ?? item.Id.ToString();

    public override async Task SaveItemAsync(EntityContainer item) 
    {
        if (item.Name is null) return;

        if (item.HasIdentity) {
            await _containerService.UpdateAsync(item.Key, item.Name, Constants.Security.SuperUserKey);
        }
        else {
            Guid? parentKey = null;
            var parentKeyAttempt = _idKeyMap.GetKeyForId(item.Id, UmbracoObjectTypes.ElementContainer);
            if (parentKeyAttempt.Success)
                parentKey = parentKeyAttempt.Result;

            await _containerService.CreateAsync(item.Key, item.Name, parentKey, Constants.Security.SuperUserKey);
        }
    }

    protected override async Task<SyncAttempt<EntityContainer>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    {
        var details = new List<uSyncChange>();

        var item = await FindItemAsync(node)
                ?? new EntityContainer(ContainedType.GetGuid());

        var name = node.GetAlias();
        if (item.Name != name)
        {
            details.AddUpdate(uSyncConstants.Xml.Name, ItemAlias(item), item.Name, name);
            item.Name = name;
        }

        var key = node.GetKey();
        if (item.Key != key) {
            details.AddUpdate(uSyncConstants.Xml.Key, ItemAlias(item), item.Key.ToString(), key.ToString());
            item.Key = key;
        }

        var sortOrder = node.Element(uSyncConstants.Xml.SortOrder).ValueOrDefault<int>(item.SortOrder);
        if (item.SortOrder != sortOrder)
        {
            details.AddUpdate(uSyncConstants.Xml.SortOrder, ItemAlias(item), item.SortOrder.ToString(), sortOrder.ToString());
            item.SortOrder = sortOrder;
        }

        return SyncAttempt<EntityContainer>.Succeed(ItemAlias(item), item, ChangeType.Import, details);
    }

    public override async Task<SyncAttempt<EntityContainer>> DeserializeSecondPassAsync(EntityContainer item, XElement node, SyncSerializerOptions options)
    {
        var details = new List<uSyncChange>();

        var parentId = await GetParentIdAsync(node, item);
        if (item.ParentId != parentId)
        {
            var parent = await GetParentAsync(node);
            if (parent is not null)
            {
                details.AddUpdate(uSyncConstants.Xml.Parent, ItemAlias(item), item.ParentId.ToString(), parentId.ToString());
                await _containerService.MoveAsync(item.Key, parent?.Key, Constants.Security.SuperUserKey);
            }
        }

        return SyncAttempt<EntityContainer>.Succeed(ItemAlias(item), item, ChangeType.Import, details); 

    }

    private async Task<ITreeEntity?> GetParentAsync(XElement node)
    {
        var parentNode = node.Element(uSyncConstants.Xml.Parent);
        if (parentNode is null) return null;

        var parentGuid = parentNode.GetKey();
        if (parentGuid == Guid.Empty) return null;

        return await _containerService.GetAsync(parentGuid);
    }

    private async Task<int> GetParentIdAsync(XElement node, EntityContainer item)
    {
        var parentNode = node.Element(uSyncConstants.Xml.Parent);
        if (parentNode is null) return item.ParentId;

        var parentGuid = parentNode.GetKey();
        if (parentGuid == Guid.Empty) return Constants.System.Root;

        var parentItem = await _containerService.GetAsync(parentGuid);
        if (parentItem is null) return item.ParentId;

        return parentItem.Id;
    }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(EntityContainer item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, ItemAlias(item), item.Level);
        node.Add(new XElement(uSyncConstants.Xml.SortOrder, item.SortOrder));

        if (item.ParentId != -1) {
            var parent = await _containerService.GetParentAsync(item);
            if (parent is not null) {
                node.Add(new XElement(uSyncConstants.Xml.Parent,
                    new XAttribute(uSyncConstants.Xml.Key, parent.Key), parent.Name ?? string.Empty));
            }
        }

        return SyncAttempt<XElement>.Succeed(ItemAlias(item), node, typeof(IElement), ChangeType.Export);       
    }
}
