using System;
using System.Collections.Generic;
using System.Linq;
using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Serializers.Dtos;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Mappers.DtoMappers
{
    public class UserMapper : IMapper<IUser, UserDto>
    {
        private readonly IUserService _userService;
        private readonly IContentService _contentService;

        public UserMapper(IUserService userService, IContentService contentService)
        {
            _userService = userService;
            _contentService = contentService;
        }

        public UserDto Map(IUser item)
        {
            return new UserDto
            {
                General = new UserGeneralSection
                {
                    Name = item.Name,
                    Username = item.Username,
                    Email = item.Email,
                    Language = item.Language,
                    UserEnabled = item.IsApproved,
                    UmbracoAccessDisabled = item.IsLockedOut,
                    StartContentNodeKey = _contentService.GetById(item.StartContentId)?.Key ?? Guid.Empty,
                    StartMediaNodeKey = _contentService.GetById(item.StartMediaId)?.Key ?? Guid.Empty,
                    UserTypeAlias = item.UserType.Alias,
                    RawPassword = item.RawPasswordValue,
                    RawPasswordAnswer = item.RawPasswordAnswerValue
                },
                AllowedSections = item.AllowedSections.ToList(),
                NodePermissions = GetNodePermissions(item)
            };
        }

        private List<NodePermission> GetNodePermissions(IUser item)
        {
            var permissions = _userService.GetPermissions(item);
            return permissions.Select(permission => new NodePermission
            {
                NodeKey = _contentService.GetById(permission.EntityId).Key, Permissions = permission.AssignedPermissions.ToList()
            }).ToList();
        }
    }
}