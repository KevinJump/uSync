using System;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;
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

        //
        // for the member type, the built in properties are created with guid's that are really int values
        // as a result the Key value you get back for them, can change between reboots. 
        //
        // here we tag on to the SerializeProperties step, and blank the Key value for any of the built in 
        // properties. 
        //
        //   this means we don't get false posistives between reboots, 
        //   it also means that these properties won't get deleted if/when they are removed - but 
        //   we limit it only to these items by listing them (so custom items in a member type will still
        //   get removed when required. 
        // 

        private static string[] buildInProperties = new string[] { 
            "umbracoMemberApproved", "umbracoMemberComments", "umbracoMemberFailedPasswordAttempts",
            "umbracoMemberLastLockoutDate", "umbracoMemberLastLogin", "umbracoMemberLastPasswordChangeDate",
            "umbracoMemberLockedOut", "umbracoMemberPasswordRetrievalAnswer", "umbracoMemberPasswordRetrievalQuestion"
        };

        protected override XElement SerializeProperties(IMemberType item)
        {
            var node = base.SerializeProperties(item);
            foreach(var property in node.Elements("GenericProperty"))
            {
                var alias = property.Element("Alias").ValueOrDefault(string.Empty);
                if (!string.IsNullOrWhiteSpace(alias) && buildInProperties.InvariantContains(alias))
                {
                    property.Element("Key").Value = Guid.Empty.ToString();
                }
            }
            return node;
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
