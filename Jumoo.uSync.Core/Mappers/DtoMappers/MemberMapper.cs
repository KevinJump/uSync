using System.Collections.Generic;
using System.Linq;
using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Serializers.Dtos;
using Umbraco.Core.Models;
using Property = Jumoo.uSync.Core.Serializers.Dtos.Property;

namespace Jumoo.uSync.Core.Mappers.DtoMappers
{
    public class MemberMapper : IMapper<IMember, MemberDto>
    {
        private readonly IUmbracoXmlContentService _umbracoXmlContentService;

        public MemberMapper(IUmbracoXmlContentService umbracoXmlContentService)
        {
            _umbracoXmlContentService = umbracoXmlContentService;
        }

        public MemberDto Map(IMember item)
        {
            return new MemberDto
            {
                General = new MemberGeneralSection
                {
                    Key = item.Key,
                    Name = item.Name,
                    Email = item.Email,
                    ContentTypeAlias = item.ContentTypeAlias,
                    Username = item.Username,
                    RawPassword = item.RawPasswordValue,
                    RawPasswordAnswer = item.RawPasswordAnswerValue
                },
                Roles = System.Web.Security.Roles.GetRolesForUser(item.Username).ToList(),
                Properties = GetMemberProperties(item)
            };
        }

        public List<Property> GetMemberProperties(IMember item)
        {
            var result = new List<Property>();
            foreach (var property in item.Properties.Where(p => p != null))
            {
                if (property.PropertyType.PropertyEditorAlias.Equals(uSyncConstants.PropertyEditorAliases.NoEdit)) continue;
                var exportIds = _umbracoXmlContentService.GetExportValue(property);
                result.Add(new Property {Alias = property.Alias, Value = exportIds });
            }

            return result;
        }
    }
}