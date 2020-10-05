using System.IO;
using System.Linq;
using System.Threading.Tasks;

using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;

namespace uSync8.BackOffice.Commands.Command
{
    [SyncCommand("Report", "report", "Returns a list of pending changes based on the uSync folder")]
    public class ReportCommand : SyncCommandServiceBase, ISyncCommand
    {
        public ReportCommand(TextReader reader, TextWriter writer,
            uSyncService uSyncService) : base(reader, writer, uSyncService)
        { }

        public async Task<SyncCommandResult> Run(string[] args)
        {
            await writer.WriteAsync("Reporting");

            var options = ParseArguments(args);

            var handlerSet = options.GetSwitchValue<string>("set", uSync.Handlers.DefaultSet);

            var results = uSyncService.Report(options.Folder,
                new SyncHandlerOptions(handlerSet, HandlerActions.Report),
                callbacks);

            await writer.WriteLineAsync($"\rReport Complete {results.Count()} items");

            foreach (var item in results.Where(x => x.Change > ChangeType.NoChange))
            {
                var changeCount = item.Details != null ? item.Details.Count() : 0;
                await writer.WriteLineAsync($"{item.Change}: {item.ItemType} - {item.Name} {changeCount}");
            }

            return SyncCommandResult.Success;
        }
    }
}
