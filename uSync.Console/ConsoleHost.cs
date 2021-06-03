using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;

using uSync8.BackOffice;
using uSync8.BackOffice.Commands;
using uSync8.BackOffice.Models;

namespace uSync.ConsoleApp
{
    /// <summary>
    ///  Host for Umbraco in the console.
    /// </summary>
    public class ConsoleHost
    {
        private readonly ConsoleRuntime runtime;
        private readonly TextReader reader;
        private readonly TextWriter writer;

        private readonly IFactory factory;

        public ConsoleHost(TextReader reader, TextWriter writer)
        {
            this.reader = reader;
            this.writer = writer;

            this.runtime = new ConsoleRuntime(reader, writer);

            var register = RegisterFactory.Create();
            factory = runtime.Boot(register);
        }

        public async Task<SyncCommandResult> Run(string[] args)
        {
            var result = SyncCommandResult.NoResult;

            // clear the first line.
            await writer.WriteAsync("\r");

            await WriteUmbracoVersion();

            if (args.Length == 0)
            {
                await WriteInteractiveHeader();
            }
            else
            {
                result = await ProcessCommand(args);
                if (result < SyncCommandResult.Complete)
                {
                    result = SyncCommandResult.Complete;
                }
            }

            while (result < SyncCommandResult.Complete)
            {
                await writer.WriteAsync("uSync>");
                var command = await reader.ReadLineAsync();

                if (!string.IsNullOrWhiteSpace(command))
                {
                    var commandArgs = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    result = await ProcessCommand(commandArgs);
                }
            }

            return result;
        }

        public async Task<SyncCommandResult> ProcessCommand(string[] args)
        {
            if (Current.RuntimeState.Level > RuntimeLevel.Install)
            {
                var alias = args[0];
                var commandFactory = factory.GetInstance<SyncCommandFactory>();
                if (commandFactory == null)
                {
                    writer.WriteLine("Cannot load the commands :(");
                    return SyncCommandResult.Error;
                }

                if (alias.InvariantEquals("help"))
                {
                    return await ShowHelp(commandFactory, args);
                }

                var command = commandFactory.GetCommand(alias);
                if (command == null)
                {
                    await writer.WriteLineAsync("Unknown command");
                    return await ShowHelp(commandFactory, args);
                }
                else
                {
                    return await command.Run(args.Skip(1).ToArray());
                }
            }
            else
            {
                var bootCommands = new BootCommands(reader, writer, factory);
                return await bootCommands.RunBootUpSteps(args);
            }
        }


        private async Task<SyncCommandResult> ShowHelp(SyncCommandFactory commandFactory, string[] args)
        {

            if (args.Length < 2)
            {
                await writer.WriteLineAsync("usage: uSync <command> [args] [options]");
                await writer.WriteLineAsync("type 'usync Help <command> for help on a specific command");
                await writer.WriteLineAsync("\nAvailable commands:\n");

                foreach (var command in commandFactory.GetAll())
                {
                    await command.ShowHelp(false);
                    await writer.WriteLineAsync();
                }

            }
            else
            {
                var command = commandFactory.GetCommand(args[1]);
                if (command != null)
                {
                    await command.ShowHelp(true);
                    await writer.WriteLineAsync();
                }
                else
                {
                    await writer.WriteLineAsync($"Unknown command {args[1]}");
                }
            }

            await writer.WriteLineAsync("For more information, visit  https://docs.jumoo.co.uk/uSync/command-line");
            return SyncCommandResult.NoResult;
        }

        private async Task WriteUmbracoVersion()
        {
            var umbracoVersion = UmbracoVersion.SemanticVersion.ToSemanticString();
            await writer.WriteLineAsync($"Umbraco : {umbracoVersion} - RuntimeState:[{Current.RuntimeState.Level}]");
        }

        /// <summary>
        ///  write out the first few lines when you load interactively.
        /// </summary>
        private async Task WriteInteractiveHeader()
        {
            var uSyncVersion = typeof(uSync8BackOffice).Assembly.GetName().Version.ToString(3);
            var addOnNames = new List<string>();
            foreach (var addOn in TypeFinder.FindClassesOfType<ISyncAddOn>())
            {
                if (Activator.CreateInstance(addOn) is ISyncAddOn instance)
                {
                    addOnNames.Add(instance.Name);
                }
            }

            await writer.WriteLineAsync($"uSync   : {uSyncVersion} [{string.Join(" ", addOnNames)}]\n");
        }

    }
}
