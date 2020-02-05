using System.IO;
using System.Linq;

using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice.Commands.Command
{

    [SyncCommand("Export", "export", "Exports setting from Umbraco")]
    public class ExportCommand : SyncCommandServiceBase, ISyncCommand
    {
        public ExportCommand(TextReader reader, TextWriter writer, 
            uSyncService uSyncService) : base(reader, writer, uSyncService)
        { }

        public SyncCommandResult Run(string[] args)
        {
            var options = ParseArguments(args);

            writer.Write("Exporting :");

            var results = uSyncService.Export(options.Folder,
                new SyncHandlerOptions(options.HandlerSet, HandlerActions.Export),
                callbacks);

            writer.WriteLine("\nExported {0} items", results.Count());

            return SyncCommandResult.Success;
        }
    }
}
