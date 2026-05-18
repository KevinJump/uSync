using Microsoft.Extensions.Logging;

using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("8AD4ED7F-9E47-4918-ACAE-146132F5AB56",
    "Element Serializer", uSyncConstants.Serialization.ElementContainer, IsTwoPass = false)]
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
        var item = await FindItemAsync(node);
        if (item is null)
        {
            // create a new one. 
            item = new EntityContainer(ContainedType.GetGuid());
        }

        item.Name = node.GetAlias();
        item.Key = node.GetKey();
        item.SortOrder = int.TryParse(node.Attribute("SortOrder")?.Value, out var sortOrder) ? sortOrder : item.SortOrder;

        var parentNode = node.Element("Parent");
        if (parentNode is not null)
        {
            var parentKey = parentNode.Attribute("Key")?.Value;
            if (Guid.TryParse(parentKey, out var parentGuid))
            {
                var parentItem = await _containerService.GetAsync(parentGuid);
                if (parentItem is not null)
                {
                    item.ParentId = parentItem.Id;
                }
                else
                {
                    logger.LogWarning("Parent with key {ParentKey} not found for container {ContainerName}", parentKey, item.Name);
                }
            }
            else
            {
                logger.LogWarning("Invalid parent key {ParentKey} for container {ContainerName}", parentKey, item.Name);
            }
        }

        return SyncAttempt<EntityContainer>.Succeed(ItemAlias(item), item, ChangeType.Import, []);

    }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(EntityContainer item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, ItemAlias(item), item.Level);
        node.Add(new XElement("SortOrder", item.SortOrder));

        if (item.ParentId != -1) {
            var parent = await _containerService.GetParentAsync(item);
            if (parent is not null) {
                node.Add(new XElement("Parent",
                    new XAttribute("Key", parent.Key)),
                    new XElement(parent.Name ?? string.Empty));
            }
        }

        return SyncAttempt<XElement>.Succeed(ItemAlias(item), node, ChangeType.Export, []);
        
    }
}
