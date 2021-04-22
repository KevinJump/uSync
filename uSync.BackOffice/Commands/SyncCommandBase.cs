using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Composing;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.Commands
{
    public class SyncCommandBase
    {
        public string Name { get; protected set; }
        public string Alias { get; protected set; }
        public string HelpText { get; protected set; }

        public string AdvancedHelp { get; protected set; }

        public bool Interactive { get; set; }

        protected readonly TextReader reader;
        protected readonly TextWriter writer;
        protected readonly uSyncSettings setting;

        public SyncCommandBase(TextReader reader, TextWriter writer)
        {
            this.reader = reader;
            this.writer = writer;

            this.setting = Current.Configs.uSync();

            var meta = GetType().GetCustomAttribute<SyncCommandAttribute>(false);
            if (meta != null)
            {
                this.Name = meta.Name;
                this.Alias = meta.Alias;
                this.HelpText = meta.HelpText;
            }
        }

        public virtual async Task ShowHelp(bool advanced)
        {
            if (!advanced)
            {
                await writer.WriteLineAsync($" {this.Alias,-18}{this.HelpText}");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(AdvancedHelp))
                {
                    await writer.WriteLineAsync("No more help availible");
                }
                else
                {
                    await writer.WriteLineAsync(AdvancedHelp);
                }
                // show advanced help
            }
        }

        protected SyncCommandOptions ParseArguments(string[] args)
        {
            var options = new SyncCommandOptions(setting.RootFolder);

            int position = 0;
            foreach (var argument in args)
            {
                var cmd = argument.Trim().ToLower();

                if (cmd.StartsWith("-"))
                {
                    var fragments = cmd.Split('=');

                    var flag = fragments[0].Substring(1);

                    if (fragments.Length > 1)
                    {
                        options.Switches[flag] = fragments[1];
                    }
                    else
                    {
                        options.Switches[flag] = true;
                    }
                }
                else
                {
                    if (position == 0)
                    {
                        options.Folder = cmd;
                        position++;
                    }
                    else
                    {
                        throw new ArgumentException($"{argument} not recognised");
                    }
                }
            }

            return options;
        }
    }
}
