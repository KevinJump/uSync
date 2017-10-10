using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Serializers.Dtos;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Mappers.DtoMappers
{
    public class MemberGroupMapper : IMapper<IMemberGroup, MemberGroupDto>
    { 
        public MemberGroupDto Map(IMemberGroup item)
        {
            return new MemberGroupDto
            {
                General = new KeyGeneralSectionBase
                {
                    Key = item.Key,
                    Name = item.Name
                }
            };
        }
    }
}