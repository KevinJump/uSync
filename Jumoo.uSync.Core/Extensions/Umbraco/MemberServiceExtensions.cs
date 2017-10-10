using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Extensions.Umbraco
{
    public static class MemberServiceExtensions
    {
        public static List<IMember> GetAllMembers(this IMemberService service)
        {
            var users = new List<IMember>();
            int totalMembers;
            var currentPage = 0;
            do
            {
                users.AddRange(service.GetAll(currentPage, 100, out totalMembers));
                currentPage++;
            } while (users.Count != totalMembers);
            return users;
        }

        public static IMember GetByName(this IMemberService service, string name)
        {
            return service.GetAllMembers().FirstOrDefault(u => u.Name == name);
        }
    }
}