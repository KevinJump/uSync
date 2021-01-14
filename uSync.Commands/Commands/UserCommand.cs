using System.IO;
using System.Threading.Tasks;

using uSync8.BackOffice.Commands;

namespace uSync.BaseCommands.Commands
{
    [SyncCommand("user", "user", "updates the admin user")]
    public class UserCommand : SyncCommandBase, ISyncCommand
    {
        private readonly SyncUserHelper userHelper;

        public UserCommand(TextReader reader, TextWriter writer,
            SyncUserHelper userHelper) 
            : base(reader, writer)
        {
            this.userHelper = userHelper;        
        }

        public async Task<SyncCommandResult> Run(string[] args)
        {
            if (args.Length == 2)
            {
                var user = args[0];
                var pwd = args[1];

                if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pwd))
                {
                    return await userHelper.SetupAdminUser(user, pwd);
                }
            }

            writer.WriteLine("missing parameters username and password.");
            return await Task.FromResult(SyncCommandResult.Error);
        }
    }
}
