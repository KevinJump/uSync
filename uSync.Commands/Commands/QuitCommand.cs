using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uSync8.BackOffice.Commands;

namespace uSync.BaseCommands
{
    [SyncCommand("Quit", "quit", "Exits uSync Command line")]
    public class QuitCommand : SyncCommandBase, ISyncCommand
    {
        public QuitCommand(TextReader reader, TextWriter writer)
            : base(reader, writer)
        {
            AdvancedHelp = HelpTextResource.Quit_Help;
        }

        public async Task<SyncCommandResult> Run(string[] args)
        {
            await writer.WriteLineAsync("Exiting...\n");
            return SyncCommandResult.Complete;
        }

    }
}
