using System.IO;
using System.Linq;
using System.Threading.Tasks;

using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice.Commands.Command
{

    [SyncCommand("Export", "export", "Exports setting from Umbraco")]
    public class ExportCommand : SyncCommandServiceBase, ISyncCommand
    {
        public ExportCommand(TextReader reader, TextWriter writer,
            uSyncService uSyncService) : base(reader, writer, uSyncService)
        { }

        public async Task<SyncCommandResult> Run(string[] args)
        {
            var options = ParseArguments(args);

            await writer.WriteAsync("Exporting :");

            var handlerSet = options.GetSwitchValue<string>("set", uSync.Handlers.DefaultSet);

            var results = uSyncService.Export(options.Folder,
                new SyncHandlerOptions(handlerSet, HandlerActions.Export),
                callbacks);

            await writer.WriteLineAsync($"\nExported {results.Count()} items");

            return SyncCommandResult.Success;
        }
    }
}
