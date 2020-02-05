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
    public class ReportCommand : SyncCommandServiceBase, ISyncCommand
    {
        public ReportCommand(TextReader reader, TextWriter writer,
            uSyncService uSyncService) : base(reader, writer, uSyncService)
        {}

        public SyncCommandResult Run(string[] args)
        {
            writer.Write("Reporting");

            var options = ParseArguments(args);

            var results = uSyncService.Report(options.Folder,
                new SyncHandlerOptions(options.HandlerSet, HandlerActions.Report),
                callbacks);

            writer.WriteLine("\rReport Complete {0} items", results.Count());

            foreach(var item in results.Where(x => x.Change > ChangeType.NoChange))
            {
                var changeCount = item.Details != null ? item.Details.Count() : 0;
                writer.WriteLine($"{item.Change}: {item.ItemType} - {item.Name} {changeCount}");
            }

            return SyncCommandResult.Success;
        }
    }
}
