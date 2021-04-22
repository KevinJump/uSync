using System;

namespace uSync.BackOffice.Commands
{
    public class SyncCommandAttribute : Attribute
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public string HelpText { get; set; }

        public SyncCommandAttribute(string name, string alias, string help)
        {
            this.Name = name;
            this.Alias = alias;
            this.HelpText = help;
        }
    }
}
