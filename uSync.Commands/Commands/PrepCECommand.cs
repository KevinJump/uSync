using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Composing;

using uSync8.BackOffice.Commands;

namespace uSync.BaseCommands.Commands
{
    [SyncCommand("Prep", "prep", "Preps a SQL CE Database if its not there")]
    public class PrepCECommand : SyncCommandBase, ISyncCommand
    {
        public PrepCECommand(TextReader reader, TextWriter writer) : base(reader, writer)
        { }

        public async Task<SyncCommandResult> Run(string[] args)
        {
            if (Current.RuntimeState.Level == RuntimeLevel.Run)
            {
                await writer.WriteLineAsync(" Umbraco is already installed");
                return SyncCommandResult.NoResult;
            }

            return SyncCommandResult.Success;
        }
    }
}
