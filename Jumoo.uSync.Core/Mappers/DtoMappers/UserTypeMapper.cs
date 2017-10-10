using System.Linq;
using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Serializers.Dtos;
using Umbraco.Core.Models.Membership;

namespace Jumoo.uSync.Core.Mappers.DtoMappers
{
    public class UserTypeMapper : IMapper<IUserType, UserTypeDto>
    { 
        public UserTypeDto Map(IUserType item)
        {
            return new UserTypeDto
            {
                General = new UserTypeGeneralSection
                {
                    Name = item.Name,
                    Alias = item.Alias
                },
                Permissions = item.Permissions.ToList()
            };
        }
    }
}