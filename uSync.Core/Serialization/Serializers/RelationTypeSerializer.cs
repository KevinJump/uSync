using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    /// <summary>
    ///  Serializer for RelationTypes and optionally the relations inside them.
    /// </summary>
    [SyncSerializer("19FA7E6D-3B88-44AA-AED4-94634C90A5B4", "RelationTypeSerializer", uSyncConstants.Serialization.RelationType)]
    public class RelationTypeSerializer
        : SyncSerializerBase<IRelationType>, ISyncSerializer<IRelationType>
    {
        private IRelationService relationService;

        public RelationTypeSerializer(IEntityService entityService,
            IRelationService relationService,
            ILogger<RelationTypeSerializer> logger)
            : base(entityService, logger)
        {
            this.relationService = relationService;
        }

        protected override SyncAttempt<IRelationType> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var key = node.GetKey();
            var alias = node.GetAlias();

            var info = node.Element("Info");

            var name = info.Element("Name").ValueOrDefault(string.Empty);
            var parentType = info.Element("ParentType").ValueOrDefault<Guid?>(null);
            var childType = info.Element("ChildType").ValueOrDefault<Guid?>(null);
            var bidirectional = info.Element("Bidirectional").ValueOrDefault(false);
            var isDependency = info.Element("IsDependency").ValueOrDefault(true);

            var item = FindItem(node);

            if (item == null)
            {
                item = CreateRelation(name, alias, bidirectional, parentType, childType, isDependency);
            }

            var details = new List<uSyncChange>();

            if (item.Key != key)
            {
                details.AddUpdate("Key", item.Key, key);
                item.Key = key;
            }

            if (item.Name != name)
            {
                details.AddUpdate("Name", item.Name, name);
                item.Name = name;
            }

            if (item.Alias != alias)
            {
                details.AddUpdate("Alias", item.Alias, alias);
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
                this.SaveItem(item);
                hasBeenSaved = true;
                message = "Relation items included";
                details.AddRange(DeserializeRelations(node, item, options));
            }

            return SyncAttempt<IRelationType>.Succeed(item.Name, item, ChangeType.Import, message, hasBeenSaved, details);
        }

        /// <summary>
        ///  Deserialize the relations for a relation type.
        /// </summary>
        private IEnumerable<uSyncChange> DeserializeRelations(XElement node, IRelationType relationType, SyncSerializerOptions options)
        {
            var changes = new List<uSyncChange>();

            var existing = relationService
                .GetAllRelationsByRelationType(relationType.Id)
                .ToList();

            var relations = node.Element("Relations");

            // do we do this, or do we remove them all!
            if (relations == null) return Enumerable.Empty<uSyncChange>();

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
                    relationService.Save(new Relation(parentItem.Id, childItem.Id, relationType));
                    changes.Add(uSyncChange.Create(relationType.Alias, parentItem.Name, childItem.Name));
                }

                newRelations.Add($"{parentItem.Id}_{childItem.Id}");
            }


            if (options.DeleteItems())
            {
                var obsolete = existing.Where(x => !newRelations.Contains($"{x.ParentId}_{x.ChildId}"));

                foreach (var obsoleteRelation in obsolete)
                {
                    changes.Add(uSyncChange.Delete(relationType.Alias, obsoleteRelation.ParentId.ToString(), obsoleteRelation.ChildId.ToString()));
                    relationService.Delete(obsoleteRelation);
                }
            }

            return changes;
        }

        /// <summary>
        ///  checks to see if this is a valid xml element for a relationType
        /// </summary>
        public override bool IsValid(XElement node)
        {
            if (node?.Element("Info")?.Element("Name") == null) return false;
            return base.IsValid(node);
        }

        protected override SyncAttempt<XElement> SerializeCore(IRelationType item, SyncSerializerOptions options)
        {
            var node = this.InitializeBaseNode(item, item.Alias);

            var isDependency = false;
            if (item is IRelationTypeWithIsDependency dependencyItem)
                isDependency = dependencyItem.IsDependency;

            node.Add(new XElement("Info",
                new XElement("Name", item.Name),
                new XElement("ParentType", GetGuidValue(item, nameof(item.ParentObjectType))),
                new XElement("ChildType", GetGuidValue(item, nameof(item.ChildObjectType))),
                new XElement("Bidirectional", item.IsBidirectional),
                new XElement("IsDependency", isDependency)));
               

            if (options.GetSetting<bool>("IncludeRelations", false))
            {
                node.Add(SerializeRelations(item));
            }

            return SyncAttempt<XElement>.SucceedIf(
                node != null,
                item.Name,
                node,
                typeof(IRelationType),
                ChangeType.Export);
        }


        private RelationType CreateRelation(string name, string alias, bool isBidrectional, Guid? parent, Guid? child, bool isDependency)
        {
            return new RelationType(name, alias, isBidrectional, parent.Value, child.Value, isDependency);
        }

        /// <summary>
        ///  gets a value from the interface that might be Guid or Guid?
        /// </summary>
        /// <remarks>
        ///  works around the interface changing v8.6 from Guid to Guid?
        /// </remarks>
        private Guid? GetGuidValue(IRelationType item, string propertyName)
        {
            var propertyInfo = item.GetType().GetProperty(propertyName);
            if (propertyInfo == null) return null;

            var value = propertyInfo.GetValue(item);
            if (value == null) return null;

            if (value is Guid guid)
            {
                return guid;
            }

            return null;
        }

        private void SetGuidValue(object item, string propertyName, Guid? value)
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
            var relations = relationService.GetAllRelationsByRelationType(item.Id);

            var node = new XElement("Relations");

            foreach (var relation in relations.OrderBy(x => x.ChildId).OrderBy(x => x.ParentId))
            {
                var relationNode = new XElement("Relation");

                var entities = relationService.GetEntitiesFromRelation(relation);

                if (entities.Item1 != null)
                    relationNode.Add(new XElement("Parent", entities.Item1.Key));

                if (entities.Item2 != null)
                    relationNode.Add(new XElement("Child", entities.Item2.Key));

                node.Add(relationNode);
            }

            return node;
        }



        // control methods.

        public override void DeleteItem(IRelationType item)
            => relationService.Delete(item);

        public override IRelationType FindItem(int id)
            => relationService.GetRelationTypeById(id);

        public override IRelationType FindItem(Guid key)
            => relationService.GetRelationTypeById(key); // ??

        public override IRelationType FindItem(string alias)
            => relationService.GetRelationTypeByAlias(alias);

        public override string ItemAlias(IRelationType item)
            => item.Alias;

        public override void SaveItem(IRelationType item)
            => relationService.Save(item);
    }
}