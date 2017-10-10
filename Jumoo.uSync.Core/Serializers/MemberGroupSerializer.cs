using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.Serialization;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Extensions.Umbraco;
using Jumoo.uSync.Core.Helpers;
using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Mappers.DtoMappers;
using Jumoo.uSync.Core.Serializers.Dtos;
using Jumoo.uSync.Core.Validation;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Serializers
{
    public class MemberGroupSerializer : SyncExtendedSerializerTwoPassBase<IMemberGroup>, ISyncSerializerTwoPass<IMemberGroup>, ISyncChangeDetail
    {
        private readonly IMemberGroupService _memberGroupService;
        private readonly IMapper<IMemberGroup, MemberGroupDto> _memberGroupMapper;

        public MemberGroupSerializer() : base(uSyncConstants.Serailization.MemberGroup)
        {
            _memberGroupService = ApplicationContext.Current.Services.MemberGroupService;
            _memberGroupMapper = new MemberGroupMapper();
        }

        public override string SerializerType => uSyncConstants.Serailization.MemberGroup;
        public override int Priority { get; } = uSyncConstants.Priority.MemberGroup;

        public override SyncAttempt<IMemberGroup> DeSearlizeSecondPass(IMemberGroup item, XElement node)
        {
            return SyncAttempt<IMemberGroup>.Succeed(node.NameFromNode(), ChangeType.NoChange);
        }

        public override SyncAttempt<XElement> SerializeCore(IMemberGroup item)
        {
            var element = new XmlSerializer(typeof(MemberGroupDto)).ToXElement(_memberGroupMapper.Map(item));
            return SyncAttempt<XElement>.Succeed(item.Name, element, typeof(IMember), ChangeType.Export);
        }

        public override SyncAttempt<IMemberGroup> DeserializeCore(XElement node)
        {
            var memberGroupDto = node.FromXElement<MemberGroupDto>();

            var memberGroup = _memberGroupService.GetByKey(memberGroupDto.General.Key) ??
                              (_memberGroupService.GetByName(memberGroupDto.General.Name) ?? new MemberGroup
                              {
                                  Key = memberGroupDto.General.Key,
                                  Name = memberGroupDto.General.Name
                              });

            _memberGroupService.Save(memberGroup);
            return SyncAttempt<IMemberGroup>.Succeed(memberGroup.Name, memberGroup, ChangeType.Import, $"MemberGroup {memberGroup.Name} deserialization has completed.");
        }

        public override bool IsUpdate(XElement node)
        {
            var memberGroupDto = node.FromXElement<MemberGroupDto>();

            var memberGroup = _memberGroupService.GetByKey(memberGroupDto.General.Key);
            if (memberGroup != null) return !memberGroupDto.Equals(_memberGroupMapper.Map(memberGroup));
            memberGroup = _memberGroupService.GetByName(memberGroupDto.General.Name);
            if (memberGroup == null) return true;

            return !memberGroupDto.Equals(_memberGroupMapper.Map(memberGroup));
        }

        public override IEnumerable<uSyncChange> GetChanges(XElement node)
        {
            var memberGroupDto = node.FromXElement<MemberGroupDto>();

            if (string.IsNullOrEmpty(node.GetSyncHash()))
                return uSyncChangeTracker.ChangeError(node.Name.LocalName);

            var memberGroup = _memberGroupService.GetByKey(memberGroupDto.General.Key);
            if (memberGroup == null)
            {
                memberGroup = _memberGroupService.GetByName(memberGroupDto.General.Name);
                if (memberGroup == null)
                    return uSyncChangeTracker.NewItem(memberGroupDto.General.Name);
            }

            var syncAttempt = Serialize(memberGroup);
            return syncAttempt.Success
                ? uSyncChangeTracker.GetChanges(node, syncAttempt.Item, "")
                : uSyncChangeTracker.ChangeError(memberGroup.Name);
        }

        public override bool Validate(XElement node)
        {
            return node.ValidateAgainstSchemaString(Schemas.MemberGroupSchema);
        }
    }
}