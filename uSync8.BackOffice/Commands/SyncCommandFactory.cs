using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.Commands
{
    public class SyncCommandFactory
    {
        private readonly SyncCommandCollection commands;

        public SyncCommandFactory(SyncCommandCollection commands)
        {
            this.commands = commands;
        }

        public ISyncCommand GetCommand(string alias)
            => commands.GetCommand(alias);

        public IEnumerable<ISyncCommand> GetAll()
            => commands;
    }
}
