using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Security;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Extensions.Umbraco
{
    public static class UserServiceExtensions
    {
        public static List<IUser> GetAllUsers(this IUserService service)
        {
            var users = new List<IUser>();
            int totalUsers;
            var currentPage = 0;
            do
            {
                users.AddRange(service.GetAll(currentPage, 100, out totalUsers));
                currentPage++;
            } while (users.Count != totalUsers);
            return users;
        }

        public static IUser GetByKey(this IUserService service, Guid key)
        {
            return service.GetAllUsers().FirstOrDefault(u => u.Key == key);
        }

        public static IUser GetByUsername(this IUserService service, string username)
        {
            return service.GetAllUsers().FirstOrDefault(u => u.Username == username);
        }
        public static IUser GetActiveUser(this IUserService service)
        {
            var userTicket = new System.Web.HttpContextWrapper(System.Web.HttpContext.Current).GetUmbracoAuthTicket();
            if (userTicket == null) throw new UnauthorizedAccessException("Cannot retrieve umbraco authentication ticket.");

            return service.GetByUsername(userTicket.Name);
        }
    }
}