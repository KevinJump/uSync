using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.Commands
{
    /// <summary>
    ///  Command Factory, used to get loaded commands
    /// </summary>
    public class SyncCommandFactory
    {
        private readonly SyncCommandCollection commands;

        public SyncCommandFactory(SyncCommandCollection commands)
        {
            this.commands = commands;
        }

        /// <summary>
        ///  get a command based on the alias 
        /// </summary>
        public ISyncCommand GetCommand(string alias)
            => commands.GetCommand(alias);

        /// <summary>
        ///  get all commands 
        /// </summary>
        public IEnumerable<ISyncCommand> GetAll()
            => commands;
    }
}
