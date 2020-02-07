﻿using System;
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

        public async Task<SyncCommandResult> Run(string[] args)
        {
            await writer.WriteAsync("Importing ");
            var options = ParseArguments(args);

            var force = options.GetSwitchValue<bool>("force", false);
            var handlerSet = options.GetSwitchValue<string>("set", uSync.Handlers.DefaultSet);

            if (force) await writer.WriteAsync("(With Force) ");            

            var result = uSyncService.Import(options.Folder, force,
                new SyncHandlerOptions(handlerSet, HandlerActions.Import),
                callbacks);

            var changeCount = result.Where(x => x.Change > ChangeType.NoChange).Count();

            await writer.WriteAsync("\n");
            await writer.WriteLineAsync($"Imported {result.Count()} items {changeCount} changes");

            return SyncCommandResult.Success;
        }

    }
}
