using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;

using Umbraco.Core;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;

using uSync8.BackOffice.Commands;

namespace uSync.BaseCommands
{
    public class SyncUserHelper
    {
        private readonly TextReader reader;
        private readonly TextWriter writer;

        private readonly IUserService userService;
        public SyncUserHelper(
            TextReader reader, TextWriter writer,
            IUserService userService)
        {
            this.reader = reader;
            this.writer = writer;

            this.userService = userService;
        }

        public bool AdminUserNeedsaUpdate(string username) {
            var adminUser = GetAdminUser();
            return adminUser.Username != username;
        }

        public async Task<SyncCommandResult> SetupAdminUser(string username, string password)
        {
            var adminUser = GetAdminUser();
            var memberUser = GetMembershipUser();

            await writer.WriteLineAsync(" Found super user ");

            try
            {
                await writer.WriteLineAsync($" Setting password {new string('x', password.Length)}");
                memberUser.ChangePassword("default", password);
            }
            catch(Exception ex) {
                await writer.WriteLineAsync(ex.ToString());
            }


            await writer.WriteLineAsync($" Setting username/email/name to {username}");

            // change the defaults.
            //
            adminUser.Username = username;
            adminUser.Email = username;
            adminUser.Name = username;
            userService.Save(adminUser);

            await writer.WriteLineAsync(" Saving user");

            return SyncCommandResult.Success;
        }

        public IUser GetAdminUser()
        {
            var user = userService.GetUserById(Constants.Security.SuperUserId);
            if (user == null)
                throw new InvalidOperationException("Could not found the super user in the database");

            return user;
        }

        public MembershipUser GetMembershipUser() { 

            var membershipUser = GetCurrentProvider()?.GetUser(Constants.Security.SuperUserId, true);
            if (membershipUser == null)
                throw new InvalidOperationException("Could not found membership entry for admin user");

            return membershipUser;

        }


        private MembershipProvider GetCurrentProvider()
           => Umbraco.Core.Security.MembershipProviderExtensions.GetUsersMembershipProvider();

    }
}
