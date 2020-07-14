using System;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("F45B5C7B-C206-4971-858B-6D349E153ACE", "MemberTypeSerializer", uSyncConstants.Serialization.MemberType)]
    public class MemberTypeSerializer : ContentTypeBaseSerializer<IMemberType>, ISyncOptionsSerializer<IMemberType>
    {
        private readonly IMemberTypeService memberTypeService;

        public MemberTypeSerializer(
            IEntityService entityService, ILogger logger,
            IDataTypeService dataTypeService,
            IMemberTypeService memberTypeService) 
            : base(entityService, logger, dataTypeService, memberTypeService, UmbracoObjectTypes.Unknown)
        {
            this.memberTypeService = memberTypeService;
        }

        protected override SyncAttempt<XElement> SerializeCore(IMemberType item, SyncSerializerOptions options)
        {
            var node = SerializeBase(item);
            var info = SerializeInfo(item);

            var parent = item.ContentTypeComposition.FirstOrDefault(x => x.Id == item.ParentId);
            if (parent != null)
            {
                info.Add(new XElement("Parent", parent.Alias,
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

            node.Add(info);
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

        protected override SyncAttempt<IMemberType> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var item = FindOrCreate(node);

            DeserializeBase(item, node);
            DeserializeTabs(item, node);
            DeserializeProperties(item, node);

            CleanTabs(item, node);

            // memberTypeService.Save(item);

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

        protected override IMemberType CreateItem(string alias, ITreeEntity parent, string extra)
        {
            var item = new MemberType(-1)
            {
                Alias = alias
            };

            if (parent != null)
            {
                if (parent is IMediaType mediaTypeParent)
                    item.AddContentType(mediaTypeParent);

                item.SetParent(parent);
            }


            return item;
        }
    }
}
