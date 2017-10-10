using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.Serialization;
using Jumoo.uSync.Core.Entities;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Helpers;
using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Mappers.DtoMappers;
using Jumoo.uSync.Core.Serializers.Dtos;
using Jumoo.uSync.Core.Validation;
using Umbraco.Core;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Serializers
{
    public class UserTypeSerializer : SyncExtendedSerializerTwoPassBase<IUserType>, ISyncSerializerTwoPass<IUserType>
    {
        private readonly IUserService _userService;
        private readonly IMapper<IUserType, UserTypeDto> _userTypeMapper;

        public UserTypeSerializer() : base(uSyncConstants.Serailization.UserType)
        {
            _userService = ApplicationContext.Current.Services.UserService;
            _userTypeMapper = new UserTypeMapper();
        }

        public override string SerializerType => uSyncConstants.Serailization.UserType;
        public override int Priority { get; } = uSyncConstants.Priority.UserType;

        public override SyncAttempt<IUserType> DeSearlizeSecondPass(IUserType item, XElement node)
        {
            return SyncAttempt<IUserType>.Succeed(node.NameFromNode(), ChangeType.Import);
        }

        public override SyncAttempt<XElement> SerializeCore(IUserType item)
        {
            var element = new XmlSerializer(typeof(UserTypeDto)).ToXElement(_userTypeMapper.Map(item));
            return SyncAttempt<XElement>.Succeed(item.Name, element, typeof(IUserType), ChangeType.Export);
        }

        public override SyncAttempt<IUserType> DeserializeCore(XElement node)
        {
            var userTypeDto = node.FromXElement<UserTypeDto>();

            var userType = _userService.GetUserTypeByAlias(userTypeDto.General.Alias) ??
                           _userService.GetUserTypeByName(userTypeDto.General.Name) ?? new UserType();

            userType.Name = userTypeDto.General.Name;
            userType.Alias = userTypeDto.General.Alias;
            userType.Permissions = userTypeDto.Permissions;

            _userService.SaveUserType(userType);

            return SyncAttempt<IUserType>.Succeed(userType.Name, userType, ChangeType.Import, $"UserType {userType.Name} deserialization has completed.");
        }

        public override bool IsUpdate(XElement node)
        {
            var userTypeDto = node.FromXElement<UserTypeDto>();

            var userType = _userService.GetUserTypeByName(userTypeDto.General.Name);
            if (userType != null) return !userTypeDto.Equals(_userTypeMapper.Map(userType));
            userType = _userService.GetUserTypeByAlias(userTypeDto.General.Alias);
            if (userType == null) return true;

            return !userTypeDto.Equals(_userTypeMapper.Map(userType));
        }

        public override IEnumerable<uSyncChange> GetChanges(XElement node)
        {
            var userTypeDto = node.FromXElement<UserTypeDto>();

            if (string.IsNullOrEmpty(node.GetSyncHash()))
                return uSyncChangeTracker.ChangeError(node.Name.LocalName);

            var userType = _userService.GetUserTypeByName(userTypeDto.General.Name);
            if (userType == null)
            {
                userType = _userService.GetUserTypeByAlias(userTypeDto.General.Alias);
                if (userType == null) return uSyncChangeTracker.NewItem(userTypeDto.General.Name);
            }

            var syncAttempt = Serialize(userType);
            return syncAttempt.Success
                ? uSyncChangeTracker.GetChanges(node, syncAttempt.Item, "")
                : uSyncChangeTracker.ChangeError(userTypeDto.General.Name);
        }

        public override bool Validate(XElement node)
        {
            return node.ValidateAgainstSchemaString(Schemas.UserTypeSchema);
        }
    }
}