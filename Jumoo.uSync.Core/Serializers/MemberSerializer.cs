using System.Collections.Generic;
using System.Linq;
using System.Web.Security;
using System.Xml.Linq;
using System.Xml.Serialization;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Extensions.Umbraco;
using Jumoo.uSync.Core.Helpers;
using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Mappers.DtoMappers;
using Jumoo.uSync.Core.Serializers.Dtos;
using Jumoo.uSync.Core.Services;
using Jumoo.uSync.Core.Validation;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Serializers
{
    public class MemberSerializer : SyncExtendedSerializerTwoPassBase<IMember>, ISyncSerializerTwoPass<IMember>
    {
        private readonly IMemberService _memberService;
        private readonly IMemberTypeService _memberTypeService;
        private readonly IUmbracoXmlContentService _umbracoXmlContentService;
        private readonly IMapper<IMember, MemberDto> _memberMapper;

        public MemberSerializer() : base(uSyncConstants.Serailization.Member)
        {
            _memberService = ApplicationContext.Current.Services.MemberService;
            _memberTypeService = ApplicationContext.Current.Services.MemberTypeService;
            _umbracoXmlContentService = new UmbracoXmlContentService();
            _memberMapper = new MemberMapper(_umbracoXmlContentService);
        }

        public override string SerializerType => uSyncConstants.Serailization.Member;
        public override int Priority { get; } = uSyncConstants.Priority.Member;

        public override SyncAttempt<IMember> DeSearlizeSecondPass(IMember item, XElement node)
        {
            var memberDto = node.FromXElement<MemberDto>();
            _memberService.DissociateRoles(new[] { item.Id }, Roles.GetRolesForUser(item.Username).ToArray());
            foreach (var role in memberDto.Roles)
            {
                _memberService.AssignRole(item.Id, role);
            }

            return SyncAttempt<IMember>.Succeed(node.NameFromNode(), ChangeType.Import);
        }

        public override SyncAttempt<XElement> SerializeCore(IMember item)
        {
            var element = new XmlSerializer(typeof(MemberDto)).ToXElement(_memberMapper.Map(item));
            return SyncAttempt<XElement>.Succeed(item.Name, element, typeof(IMember), ChangeType.Export);
        }

        private SyncAttempt<IMember> UpdateProperties(IMember member, MemberDto dto)
        {
            var memberType = _memberTypeService.Get(dto.General.ContentTypeAlias);

            foreach (var property in dto.Properties)
            {
                var propertyType = memberType.PropertyTypes.FirstOrDefault(pt => pt.Alias == property.Alias);
                if (propertyType == null)
                {
                    return SyncAttempt<IMember>.Fail(member.Name, member, ChangeType.ImportFail,
                        $"{memberType.Alias} doesnt have the property '{property.Alias}'. Cannot update member properties.");
                }
                if (propertyType.PropertyEditorAlias.Equals(uSyncConstants.PropertyEditorAliases.NoEdit)) continue;
                var newValue = _umbracoXmlContentService.GetImportValue(propertyType, _umbracoXmlContentService.GetImportXml(property.Value));
                member.SetValue(property.Alias, newValue);
            }

            return SyncAttempt<IMember>.Succeed(string.Empty, ChangeType.Import);
        }

        public override SyncAttempt<IMember> DeserializeCore(XElement node)
        {
            var memberDto = node.FromXElement<MemberDto>();

            var member = _memberService.GetByKey(memberDto.General.Key) ??
                         _memberService.GetByName(memberDto.General.Name);
            if (member != null)
            {
                if (member.ContentTypeAlias != memberDto.General.ContentTypeAlias)
                {
                    _memberService.Delete(member);
                    LogHelper.Info<SyncExtendedSerializerBase<IMember>>(
                        $"Member with the same name name - {member.Name} but different MemberType - {member.ContentTypeAlias} has been found. Removing existing member.");
                }
            }

            if (member == null)
            {
                member = _memberService.CreateMember(
                    memberDto.General.Username,
                    memberDto.General.Email,
                    memberDto.General.Name,
                    memberDto.General.ContentTypeAlias);

                member.RawPasswordValue = memberDto.General.RawPassword;
                member.RawPasswordValue = memberDto.General.RawPasswordAnswer;
            }

            member.Name = memberDto.General.Name;
            member.Key = memberDto.General.Key;
            member.Username = memberDto.General.Username;
            member.Email = memberDto.General.Email;

            var result = UpdateProperties(member, memberDto);
            if (!result.Success) return result;

            _memberService.Save(member);
            return SyncAttempt<IMember>.Succeed(member.Name, member, ChangeType.Import, $"Member {member.Name} deserialization has completed.");
        }

        public override bool IsUpdate(XElement node)
        {
            var memberDto = node.FromXElement<MemberDto>();

            var member = _memberService.GetByKey(memberDto.General.Key);
            if (member != null) return !memberDto.Equals(_memberMapper.Map(member));
            member = _memberService.GetByName(memberDto.General.Name);
            if (member == null) return true;

            return !memberDto.Equals(_memberMapper.Map(member));
        }

        public override IEnumerable<uSyncChange> GetChanges(XElement node)
        {
            var memberDto = node.FromXElement<MemberDto>();

            if (string.IsNullOrEmpty(node.GetSyncHash()))
                return uSyncChangeTracker.ChangeError(node.Name.LocalName);

            var member = _memberService.GetByKey(memberDto.General.Key);
            if (member == null)
            {
                member = _memberService.GetByName(memberDto.General.Name);
                if (member == null) return uSyncChangeTracker.NewItem(memberDto.General.Name);
            }

            var syncAttempt = Serialize(member);
            return syncAttempt.Success
                ? uSyncChangeTracker.GetChanges(node, syncAttempt.Item, "")
                : uSyncChangeTracker.ChangeError(memberDto.General.Name);
        }

        public override bool Validate(XElement node)
        {
            return node.ValidateAgainstSchemaString(Schemas.MemberSchema);
        }
    }
}