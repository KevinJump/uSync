using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("F45B5C7B-C206-4971-858B-6D349E153ACE", "MemberTypeSerializer", uSyncConstants.Serialization.MemberType)]
    public class MemberTypeSerializer : ContentTypeBaseSerializer<IMemberType>,
        ISyncSerializer<IMemberType>
    {

        private readonly IMemberTypeService memberTypeService;

        public MemberTypeSerializer(
            IEntityService entityService, 
            IDataTypeService dataTypeService,
            IMemberTypeService memberTypeService) 
            : base(entityService, dataTypeService)
        {
            this.memberTypeService = memberTypeService;
        }

        protected override SyncAttempt<XElement> SerializeCore(IMemberType item)
        {
            var node = SerializeBase(item);
            var info = SerializeInfo(item);

            var parent = item.ContentTypeComposition.FirstOrDefault(x => x.Id == item.ParentId);
            if (parent != null)
            {
                info.Add(new XElement("Master", parent.Alias,
                    new XAttribute("Key", parent.Key)));
            }
            else if (item.Level != 1)
            {
                // in a folder
                var folderNode = GetFolderNode(memberTypeService.GetContainers(item));
                if (folderNode != null)
                    info.Add(folderNode);
            }

            info.Add(SerializeCompostions((ContentTypeCompositionBase)item));

            node.Add(SerializeProperties(item));
            node.Add(SerializeStructure(item));
            node.Add(SerializeTabs(item));

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IMediaType), ChangeType.Export);

        }

        protected override void SerializeExtraProperties(XElement node, IMemberType item, PropertyType property)
        {
            node.Add(new XElement("CanEdit", item.MemberCanEditProperty(property.Alias)));
            node.Add(new XElement("CanView", item.MemberCanViewProperty(property.Alias)));
            node.Add(new XElement("IsSensitive", item.IsSensitiveProperty(property.Alias)));
        }

        protected override SyncAttempt<IMemberType> DeserializeCore(XElement node)
        {
            if (IsValid(node))
                throw new ArgumentException("Invalid XML Format");

            var item = FindOrCreate(node);

            DeserializeBase(item, node);
            DeserializeTabs(item, node);
            DeserializeProperties(item, node);

            CleanTabs(item, node);

            memberTypeService.Save(item);

            return SyncAttempt<IMemberType>.Succeed(
                item.Name,
                item,
                ChangeType.Import,
                "");
        }

        protected override void DeserializeExtraProperties(IMemberType item, PropertyType property, XElement node)
        {
            item.SetMemberCanEditProperty(property.Alias, node.Element("CanEdit").ValueOrDefault(false));
            item.SetMemberCanViewProperty(property.Alias, node.Element("CanView").ValueOrDefault(false));
            item.SetIsSensitiveProperty(property.Alias, node.Element("IsSensitive").ValueOrDefault(true));

        }

        protected override IMemberType LookupByAlias(string alias)
            => memberTypeService.Get(alias);

        protected override IMemberType LookupById(int id)
            => memberTypeService.Get(id);

        protected override IMemberType LookupByKey(Guid key)
            => memberTypeService.Get(key);

        protected override Attempt<OperationResult<OperationResultType, EntityContainer>> CreateContainer(int parentId, string name)
            => memberTypeService.CreateContainer(parentId, name);

        protected override EntityContainer GetContainer(Guid key)
            => memberTypeService.GetContainer(key);

        protected override IEnumerable<EntityContainer> GetContainers(string folder, int level)
            => memberTypeService.GetContainers(folder, level);

        
        protected override bool PropertyExistsOnComposite(IContentTypeBase item, string alias)
        {
            var allTypes = memberTypeService.GetAll().ToList();

            var allProperties = allTypes
                .Where(x => x.ContentTypeComposition.Any(y => y.Id == item.Id))
                .Select(x => x.PropertyTypes)
                .ToList();

            return allProperties.Any(x => x.Any(y => y.Alias == alias));

        }

        protected override IMemberType CreateItem(string alias, IMemberType parent, ITreeEntity treeItem)
        {
            var item = new MemberType(-1)
            {
                Alias = alias
            };

            if (parent != null)
                item.AddContentType(parent);

            item.SetParent(treeItem);

            return item;
        }
    }
}
