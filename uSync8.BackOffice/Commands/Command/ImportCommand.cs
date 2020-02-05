using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;

namespace uSync8.BackOffice.Commands.Command
{
    [SyncCommand("Import", "import", "Imports uSync settings into Umbraco")]
    public class ImportCommand : SyncCommandServiceBase, ISyncCommand
    {
        public ImportCommand(TextReader reader, TextWriter writer, 
            uSyncService uSyncService) : base(reader, writer, uSyncService)
        { }

        public SyncCommandResult Run(string[] args)
        {
            writer.Write("Importing ");
            var options = ParseArguments(args);

            if (options.Force)
            {
                writer.Write("(With Force) ");
            }
            var result = uSyncService.Import(options.Folder, options.Force,
                new SyncHandlerOptions(options.HandlerSet, HandlerActions.Import),
                callbacks);
            
            writer.Write("\n");
            writer.WriteLine("Imported {0} items {1} changes",
                result.Count(), result.Where(x => x.Change > ChangeType.NoChange).Count());

            return SyncCommandResult.Success;
        }

    }
}
