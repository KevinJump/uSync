using System.IO;
using System.Reflection;

using Umbraco.Core.Composing;

using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.Commands
{
    public class SyncCommandBase
    {
        public string Name { get; protected set; }
        public string Alias { get; protected set; }
        public string HelpText { get; protected set; }
        public bool Interactive { get;set; }

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

        public virtual void ShowHelp(bool advanced)
        {
            if (!advanced)
            {
                writer.WriteLine($" {this.Alias,-18}{this.HelpText}");
            }
            else
            {
                // show advanced help
            }
        }

        protected SyncCommandOptions ParseArguments(string[] args)
        {
            var options = new SyncCommandOptions(setting.RootFolder);

            for (int p = 0; p < args.Length; p++)
            {
                var cmd = args[p].Trim().ToLower();
                
                if (cmd.StartsWith("-"))
                {
                    var fragments = cmd.Split('=');
                    switch (fragments[0].Substring(1))
                    {
                        case "force":
                            options.Force = true;
                            break;
                        case "set":
                            options.HandlerSet = fragments[1];
                            break;
                    }
                }
                else
                {
                    if (p == 0)
                    {
                        options.Folder = cmd;
                    }
                }
            }

            return options;
        }
    }
}
