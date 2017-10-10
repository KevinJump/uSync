using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Extensions.Umbraco;
using Jumoo.uSync.Core.Helpers;
using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Mappers.DtoMappers;
using Jumoo.uSync.Core.Validation;
using Umbraco.Core;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Serializers.Dtos
{
    public class UserSerializer : SyncExtendedSerializerTwoPassBase<IUser>, ISyncSerializerTwoPass<IUser>
    {
        private readonly IUserService _userService;
        private readonly IContentService _contentService;
        private readonly IMapper<IUser, UserDto> _userMapper;

        public UserSerializer() : base(uSyncConstants.Serailization.User)
        {
            _userService = ApplicationContext.Current.Services.UserService;
            _contentService = ApplicationContext.Current.Services.ContentService;
            _userMapper = new UserMapper(_userService, _contentService);
        }

        public override string SerializerType => uSyncConstants.Serailization.User;
        public override int Priority { get; } = uSyncConstants.Priority.User;

        public override SyncAttempt<IUser> DeSearlizeSecondPass(IUser item, XElement node)
        {
            var userDto = node.FromXElement<UserDto>();
            var result = UpdateNodePermissions(item, userDto);
            return !result.Success
                ? result
                : SyncAttempt<IUser>.Succeed(node.NameFromNode(), ChangeType.Import);
        }

        public override SyncAttempt<XElement> SerializeCore(IUser item)
        {
            var element = new XmlSerializer(typeof(UserDto)).ToXElement(_userMapper.Map(item));
            return SyncAttempt<XElement>.Succeed(item.Name, element, typeof(IUser), ChangeType.Export);
        }

        private SyncAttempt<IUser> UpdateAllowedSections(IUser user, UserDto dto)
        {
            foreach (var allowedSection in user.AllowedSections)
            {
                if (dto.AllowedSections.Any(a => a.Equals(allowedSection))) continue;
                user.RemoveAllowedSection(allowedSection);
            }

            foreach (var actualSection in dto.AllowedSections)
            {
                if (user.AllowedSections.Any(a => a.Equals(actualSection))) continue;
                user.AddAllowedSection(actualSection);
            }

            return SyncAttempt<IUser>.Succeed(string.Empty, ChangeType.Import);
        }

        private SyncAttempt<IUser> UpdateNodePermissions(IUser user, UserDto dto)
        {
            var nodes = _contentService.GetContentDescendants().ToList();
            _userService.ReplaceUserPermissions(user.Id, new char[] { }, nodes.Select(cn => cn.Id).ToArray());

            foreach (var nodePermission in dto.NodePermissions)
            {
                var contentNode =
                    _contentService.GetById(nodePermission.NodeKey);
                var permissions =
                    nodePermission.Permissions.Select(e => e[0]);
                _userService.ReplaceUserPermissions(user.Id, permissions, contentNode.Id);
            }

            return SyncAttempt<IUser>.Succeed(string.Empty, ChangeType.Import);
        }

        public override SyncAttempt<IUser> DeserializeCore(XElement node)
        {
            var userDto = node.FromXElement<UserDto>();

            var user = _userService.GetByUsername(userDto.General.Username);

            if (user == null)
            {
                user = _userService.CreateUserWithIdentity(userDto.General.Username, userDto.General.Email,
                    _userService.GetUserTypeByAlias(userDto.General.UserTypeAlias));

                user.RawPasswordValue = userDto.General.RawPassword;
                user.RawPasswordAnswerValue = userDto.General.RawPasswordAnswer;
            }

            user.Name = userDto.General.Name;
            user.Username = userDto.General.Username;
            user.Email = userDto.General.Email;
            user.Language = userDto.General.Language;
            user.IsApproved = userDto.General.UserEnabled;
            user.IsLockedOut = userDto.General.UmbracoAccessDisabled;
            if (userDto.General.StartContentNodeKey != Guid.Empty)
            {
                var contentNode = _contentService.GetById(userDto.General.StartContentNodeKey);
                if (contentNode == null)
                {
                    return SyncAttempt<IUser>.Fail(user.Name, user, ChangeType.Import, $"Cannot set StartupContentNode for User {user.Name} since node with key {userDto.General.StartContentNodeKey} desnot exist.");
                }
                user.StartContentId = contentNode.Id;
            }
            if (userDto.General.StartMediaNodeKey != Guid.Empty)
            {
                var mediaNode = _contentService.GetById(userDto.General.StartMediaNodeKey);
                if (mediaNode == null)
                {
                    return SyncAttempt<IUser>.Fail(user.Name, user, ChangeType.Import, $"Cannot set StartupMediaNode for User {user.Name} since media with key {userDto.General.StartMediaNodeKey} desnot exist.");
                }
                user.StartMediaId = mediaNode.Id;
            }

            var result = UpdateAllowedSections(user, userDto);
            if (!result.Success) return result;

            _userService.Save(user);

            return SyncAttempt<IUser>.Succeed(user.Name, user, ChangeType.Import, $"User {user.Name} deserialization has completed.");
        }

        public override bool Validate(XElement node)
        {
            return node.ValidateAgainstSchemaString(Schemas.UserSchema);
        }

        public override bool IsUpdate(XElement node)
        {
            var userDto = node.FromXElement<UserDto>();

            var user = _userService.GetByUsername(userDto.General.Username);
            if (user == null) return true;

            return !userDto.Equals(_userMapper.Map(user));
        }

        public override IEnumerable<uSyncChange> GetChanges(XElement node)
        {
            var userDto = node.FromXElement<UserDto>();

            if (string.IsNullOrEmpty(node.GetSyncHash()))
                return uSyncChangeTracker.ChangeError(node.Name.LocalName);

            var user = _userService.GetByUsername(userDto.General.Username);
            if (user == null) return uSyncChangeTracker.NewItem(userDto.General.Name);

            var syncAttempt = Serialize(user);
            return syncAttempt.Success
                ? uSyncChangeTracker.GetChanges(node, syncAttempt.Item, "")
                : uSyncChangeTracker.ChangeError(userDto.General.Name);
        }
    }
}