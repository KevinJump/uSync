using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("F45B5C7B-C206-4971-858B-6D349E153ACE", "MemberTypeSerializer", uSyncConstants.Serialization.MemberType)]
    public class MemberTypeSerializer : ContentTypeBaseSerializer<IMemberType>, ISyncSerializer<IMemberType>
    {
        private readonly IMemberTypeService memberTypeService;

        public MemberTypeSerializer(
            IUmbracoVersion umbracoVersion,
            IEntityService entityService, ILogger<MemberTypeSerializer> logger,
            IDataTypeService dataTypeService,
            IMemberTypeService memberTypeService,
            IShortStringHelper shortStringHelper)
            : base(umbracoVersion, entityService, logger, dataTypeService, memberTypeService, UmbracoObjectTypes.Unknown, shortStringHelper)
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
                var folderNode = GetFolderNode(item); //TODO: Cache this call.
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

        protected override void SerializeExtraProperties(XElement node, IMemberType item, IPropertyType property)
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

        private static Dictionary<string, string> buildInProperties = new Dictionary<string, string>()
        {
            {  "umbracoMemberApproved", "e79dccfb-0000-0000-0000-000000000000" },
            {  "umbracoMemberComments", "2a280588-0000-0000-0000-000000000000" },
            {  "umbracoMemberFailedPasswordAttempts", "0f2ea539-0000-0000-0000-000000000000" },
            {  "umbracoMemberLastLockoutDate", "3a7bc3c6-0000-0000-0000-000000000000" },
            {  "umbracoMemberLastLogin", "b5e309ba-0000-0000-0000-000000000000" },
            {  "umbracoMemberLastPasswordChangeDate", "ded56d3f-0000-0000-0000-000000000000" },
            {  "umbracoMemberLockedOut", "c36093d2-0000-0000-0000-000000000000" },
            {  "umbracoMemberPasswordRetrievalAnswer", "9700bd39-0000-0000-0000-000000000000" },
            {  "umbracoMemberPasswordRetrievalQuestion", "e2d9286a-0000-0000-0000-000000000000" },
        };

        protected override XElement SerializeProperties(IMemberType item)
        {
            var node = base.SerializeProperties(item);
            foreach (var property in node.Elements("GenericProperty"))
            {
                var alias = property.Element("Alias").ValueOrDefault(string.Empty);
                if (!string.IsNullOrWhiteSpace(alias) && buildInProperties.ContainsKey(alias))
                {
                    var key = buildInProperties[alias];
                    if (!item.Alias.InvariantEquals("Member"))
                    {
                        key = $"{item.Alias}{alias}".GetDeterministicHashCode().ToGuid().ToString();
                    }
                    property.Element("Key").Value = key;
                }
            }
            return node;
        }

        protected override SyncAttempt<IMemberType> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var attempt = FindOrCreate(node);
            if (!attempt.Success)
                throw attempt.Exception;

            var item = attempt.Result;

            var details = new List<uSyncChange>();

            details.AddRange(DeserializeBase(item, node));
            details.AddRange(DeserializeTabs(item, node));
            details.AddRange(DeserializeProperties(item, node, options));

            CleanTabs(item, node, options);

            // memberTypeService.Save(item);

            return SyncAttempt<IMemberType>.Succeed(item.Name, item, ChangeType.Import, details);
        }

        protected override IEnumerable<uSyncChange> DeserializeExtraProperties(IMemberType item, IPropertyType property, XElement node)
        {
            var changes = new List<uSyncChange>();

            var canEdit = node.Element("CanEdit").ValueOrDefault(false);
            if (item.MemberCanEditProperty(property.Alias) != canEdit)
            {
                changes.AddUpdate("CanEdit", !canEdit, canEdit, $"{property.Alias}/CanEdit");
                item.SetMemberCanEditProperty(property.Alias, canEdit);
            }

            var canView = node.Element("CanView").ValueOrDefault(false);
            if (item.MemberCanViewProperty(property.Alias) != canView)
            {
                changes.AddUpdate("CanView", !canView, canView, $"{property.Alias}/CanView");
                item.SetMemberCanViewProperty(property.Alias, canView);
            }

            var isSensitive = node.Element("IsSensitive").ValueOrDefault(true);
            if (item.IsSensitiveProperty(property.Alias) != isSensitive)
            {
                changes.AddUpdate("IsSensitive", !isSensitive, isSensitive, $"{property.Alias}/IsSensitive");
                item.SetIsSensitiveProperty(property.Alias, isSensitive);
            }

            return changes;
        }

        protected override Attempt<IMemberType> CreateItem(string alias, ITreeEntity parent, string extra)
        {
            var item = new MemberType(shortStringHelper, -1)
            {
                Alias = alias
            };

            if (parent != null)
            {
                if (parent is IMediaType mediaTypeParent)
                    item.AddContentType(mediaTypeParent);

                item.SetParent(parent);
            }


            return Attempt.Succeed((IMemberType)item);
        }
    }
}
