using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.ContentEdition.Serializers
{
    /// <summary>
    ///  Serializer for RelationTypes and optionally the relations inside them.
    /// </summary>
    [SyncSerializer("19FA7E6D-3B88-44AA-AED4-94634C90A5B4", "RelationTypeSerializer", uSyncConstants.Serialization.RelationType)]
    public class RelationTypeSerializer
        : SyncSerializerBase<IRelationType>, ISyncOptionsSerializer<IRelationType>
    {
        private IRelationService relationService;

        public RelationTypeSerializer(IEntityService entityService, 
            IRelationService relationService,
            ILogger logger) 
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
            var parentType = info.Element("ParentType").ValueOrDefault(Guid.Empty);
            var childType = info.Element("ChildType").ValueOrDefault(Guid.Empty);
            var bidirectional = info.Element("Bidirectional").ValueOrDefault(false);

            var item = FindItem(node);

            if (item == null)
            {
                item = new RelationType(childType, parentType, alias);
            }

            var changes = new List<uSyncChange>();

            if (item.Key != key)
                item.Key = key;

            if (item.Name != name)
                item.Name = name;

            if (item.Alias != alias)
                item.Alias = alias;

            if (item.ParentObjectType != parentType)
                item.ParentObjectType = parentType;

            if (item.ChildObjectType != childType)
                item.ChildObjectType = childType;

            if (item.IsBidirectional = bidirectional)
                item.IsBidirectional = bidirectional;

            var hasBeenSaved = false;

            if (options.GetSetting<bool>("IncludeRelations", true))
            {
                // we have to save before we can add the relations. 
                this.SaveItem(item);
                hasBeenSaved = true;
                changes.AddRange(DeserializeRelations(node, item));
            }

            var attempt = SyncAttempt<IRelationType>.Succeed(item.Name, item, ChangeType.Import, hasBeenSaved);
            attempt.Details = changes;
            return attempt;
        }

        /// <summary>
        ///  Deserialize the relations for a relation type.
        /// </summary>
        private IEnumerable<uSyncChange> DeserializeRelations(XElement node, IRelationType relationType)
        {
            var changes = new List<uSyncChange>();

            var existing = relationService
                .GetAllRelationsByRelationType(relationType.Id)
                .ToList();

            var relations = node.Element("Relations");

            // do we do this, or do we remove them all!
            if (relations == null) return Enumerable.Empty<uSyncChange>();

            var newRelations = new List<string>();

            foreach(var relationNode in relations.Elements("Relation"))
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

            var obsolete = existing.Where(x => !newRelations.Contains($"{x.ParentId}_{x.ChildId}"));

            foreach(var obsoleteRelation in obsolete)
            {
                changes.Add(uSyncChange.Delete(relationType.Alias, obsoleteRelation.ParentId.ToString(), obsoleteRelation.ChildId.ToString()));
                relationService.Delete(obsoleteRelation);
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

            node.Add(new XElement("Info",
                new XElement("Name", item.Name),
                new XElement("ParentType", item.ParentObjectType),
                new XElement("ChildType", item.ChildObjectType),
                new XElement("Bidirectional", item.IsBidirectional)));


            if (options.GetSetting<bool>("IncludeRelations", true))
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

        private XElement SerializeRelations(IRelationType item)
        {
            var relations = relationService.GetAllRelationsByRelationType(item.Id);

            var node = new XElement("Relations");

            foreach(var relation in relations.OrderBy(x => x.ChildId).OrderBy(x=> x.ParentId))
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

        protected override void DeleteItem(IRelationType item)
            => relationService.Delete(item);

        protected override IRelationType FindItem(Guid key)
            => relationService.GetRelationTypeById(key); // ??

        protected override IRelationType FindItem(string alias)
            => relationService.GetRelationTypeByAlias(alias);

        protected override string ItemAlias(IRelationType item)
            => item.Alias;

        protected override void SaveItem(IRelationType item)
            => relationService.Save(item);
    }
}
