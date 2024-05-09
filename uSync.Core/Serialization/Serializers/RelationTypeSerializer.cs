using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Org.BouncyCastle.Crypto.Digests;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

/// <summary>
///  Serializer for RelationTypes and optionally the relations inside them.
/// </summary>
[SyncSerializer("19FA7E6D-3B88-44AA-AED4-94634C90A5B4", "RelationTypeSerializer", uSyncConstants.Serialization.RelationType)]
public class RelationTypeSerializer
    : SyncSerializerBase<IRelationType>, ISyncSerializer<IRelationType>
{
    private readonly IRelationService _relationService;

    public RelationTypeSerializer(IEntityService entityService,
        IRelationService relationService,
        ILogger<RelationTypeSerializer> logger)
        : base(entityService, logger)
    {
        this._relationService = relationService;
    }

	protected override async Task<SyncAttempt<IRelationType>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
	{
        var key = node.GetKey();
        var alias = node.GetAlias();

        var info = node.Element("Info");

        var name = info?.Element("Name").ValueOrDefault(string.Empty) ?? node.GetAlias();
        var parentType = info?.Element("ParentType").ValueOrDefault<Guid?>(null);
        var childType = info?.Element("ChildType").ValueOrDefault<Guid?>(null);
        var bidirectional = info?.Element("Bidirectional").ValueOrDefault(false) ?? false;
        var isDependency = info?.Element("IsDependency").ValueOrDefault(true) ?? true;

        var item = await FindItemAsync(node);

        item ??= CreateRelation(name, alias, bidirectional, parentType, childType, isDependency);

        var details = new List<uSyncChange>();

        if (item.Key != key)
        {
            details.AddUpdate(uSyncConstants.Xml.Key, item.Key, key);
            item.Key = key;
        }

        if (item.Name != name)
        {
            details.AddUpdate(uSyncConstants.Xml.Name, item.Name ?? item.Alias, name);
            item.Name = name;
        }

        if (item.Alias != alias)
        {
            details.AddUpdate(uSyncConstants.Xml.Alias, item.Alias, alias);
            item.Alias = alias;
        }

        var currentParentType = GetGuidValue(item, nameof(item.ParentObjectType));
        if (currentParentType != parentType)
        {
            details.AddUpdate("ParentType", currentParentType, parentType);
            SetGuidValue(item, nameof(item.ParentObjectType), parentType);
        }

        var currentChildType = GetGuidValue(item, nameof(item.ChildObjectType));
        if (currentChildType != childType)
        {
            details.AddUpdate("ChildType", currentChildType, childType);
            SetGuidValue(item, nameof(item.ChildObjectType), childType);
        }

        if (item.IsBidirectional = bidirectional)
        {
            details.AddUpdate("Bidirectional", item.IsBidirectional, bidirectional);
            item.IsBidirectional = bidirectional;
        }

        var hasBeenSaved = false;
        var message = "";
        if (options.GetSetting<bool>("IncludeRelations", false))
        {
            // we have to save before we can add the relations. 
            await this.SaveItemAsync(item, options.UserKey);
            hasBeenSaved = true;
            message = "Relation items included";
            details.AddRange(DeserializeRelations(node, item, options));
        }

        return SyncAttempt<IRelationType>.Succeed(item.Name, item, ChangeType.Import, message, hasBeenSaved, details);
    }

    /// <summary>
    ///  Deserialize the relations for a relation type.
    /// </summary>
    private List<uSyncChange> DeserializeRelations(XElement node, IRelationType relationType, SyncSerializerOptions options)
    {
        var changes = new List<uSyncChange>();

        var existing = _relationService
            .GetAllRelationsByRelationType(relationType.Id)?
            .ToList() ?? [];

        var relations = node.Element("Relations");

        // do we do this, or do we remove them all!
        if (relations == null) return [];

        var newRelations = new List<string>();

        foreach (var relationNode in relations.Elements("Relation"))
        {
            var parentKey = relationNode.Element("Parent").ValueOrDefault(Guid.Empty);
            var childKey = relationNode.Element("Child").ValueOrDefault(Guid.Empty);

            if (parentKey == Guid.Empty || childKey == Guid.Empty) continue;

            var parentItem = entityService.Get(parentKey);
            var childItem = entityService.Get(childKey);

            if (parentItem == null || childItem == null) continue;

            if (!existing.Any(x => x.ParentId == parentItem.Id && x.ChildId == childItem.Id))
            {
                // missing from the current list... add it.
                _relationService.Save(new Relation(parentItem.Id, childItem.Id, relationType));
                changes.Add(uSyncChange.Create(
                    relationType.Alias,
                    parentItem.Name ?? parentItem.Id.ToString(),
                    childItem.Name ?? childItem.Id.ToString()));
            }

            newRelations.Add($"{parentItem.Id}_{childItem.Id}");
        }


        if (options.DeleteItems())
        {
            var obsolete = existing.Where(x => !newRelations.Contains($"{x.ParentId}_{x.ChildId}"));

            foreach (var obsoleteRelation in obsolete)
            {
                changes.Add(uSyncChange.Delete(relationType.Alias, obsoleteRelation.ParentId.ToString(), obsoleteRelation.ChildId.ToString()));
                _relationService.Delete(obsoleteRelation);
            }
        }

        return changes;
    }

    /// <summary>
    ///  checks to see if this is a valid xml element for a relationType
    /// </summary>
    public override bool IsValid(XElement node)
    {
        if (node?.Element(uSyncConstants.Xml.Info)?.Element(uSyncConstants.Xml.Name) == null) return false;
        return base.IsValid(node);
    }

	protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(IRelationType item, SyncSerializerOptions options)
	{
        var node = this.InitializeBaseNode(item, item.Alias);

        var isDependency = false;
        if (item is IRelationTypeWithIsDependency dependencyItem)
            isDependency = dependencyItem.IsDependency;

        node.Add(new XElement(uSyncConstants.Xml.Info,
            new XElement(uSyncConstants.Xml.Name, item.Name),
            new XElement("ParentType", GetGuidValue(item, nameof(item.ParentObjectType))),
            new XElement("ChildType", GetGuidValue(item, nameof(item.ChildObjectType))),
            new XElement("Bidirectional", item.IsBidirectional),
            new XElement("IsDependency", isDependency)));


        if (options.GetSetting<bool>("IncludeRelations", false))
        {
            node.Add(SerializeRelations(item));
        }

        return await Task.FromResult(SyncAttempt<XElement>.SucceedIf(
            node != null,
            item.Name ?? item.Alias,
            node,
            typeof(IRelationType),
            ChangeType.Export));
    }


    private static RelationType CreateRelation(string name, string alias, bool isBidirectional, Guid? parent, Guid? child, bool isDependency)
        => new(name, alias, isBidirectional, parent, child, isDependency);

    /// <summary>
    ///  gets a value from the interface that might be GUID or GUID?
    /// </summary>
    /// <remarks>
    ///  works around the interface changing v8.6 from GUID to GUID?
    /// </remarks>
    private static Guid? GetGuidValue(IRelationType item, string propertyName)
    {
        var propertyInfo = item.GetType().GetProperty(propertyName);
        if (propertyInfo is null) return null;

        var value = propertyInfo.GetValue(item);
        if (value is null) return null;
        if (value is Guid guid) return guid;


        return null;
    }

    private static void SetGuidValue(object item, string propertyName, Guid? value)
    {
        var propertyInfo = item.GetType().GetProperty(propertyName);
        if (propertyInfo == null) return;

        if (propertyInfo.PropertyType == typeof(Guid?))
        {
            propertyInfo.SetValue(item, value);
        }
        else if (propertyInfo.PropertyType == typeof(Guid) && value != null)
        {
            propertyInfo.SetValue(item, value.Value);
        }
    }


    private XElement SerializeRelations(IRelationType item)
    {
        var relations = _relationService.GetAllRelationsByRelationType(item.Id);

        var node = new XElement("Relations");
        if (relations is null) return node;

        foreach (var relation in relations.OrderBy(x => x.ChildId).OrderBy(x => x.ParentId))
        {
            var relationNode = new XElement("Relation");

            var entities = _relationService.GetEntitiesFromRelation(relation);

            if (entities?.Item1 is not null)
                relationNode.Add(new XElement("Parent", entities.Item1.Key));

            if (entities?.Item2 is not null)
                relationNode.Add(new XElement("Child", entities.Item2.Key));

            node.Add(relationNode);
        }

        return node;
    }



    // control methods.

    public override Task DeleteItemAsync(IRelationType? item, Guid userKey)
    {
        if (item is not null)
            _relationService.Delete(item);

        return Task.CompletedTask;
	}


	public override Task<IRelationType?> FindItemAsync(Guid key)
		=> Task.FromResult(_relationService.GetRelationTypeById(key));

    public override Task<IRelationType?> FindItemAsync(string alias)
        => Task.FromResult(_relationService.GetRelationTypeByAlias(alias));

    public override string ItemAlias(IRelationType item)
        => item.Alias;

	public override Task SaveItemAsync(IRelationType? item, Guid userKey)
	{
        if (item is not null)   
			_relationService.Save(item);

        return Task.CompletedTask;
	}
}