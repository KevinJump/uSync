namespace uSync8.BackOffice.Commands
{
    public class SyncCommandOptions
    {

        public string Folder { get; set; }
        public bool Force { get; set; }
        public string HandlerSet { get; set; }

        public SyncCommandOptions(string folder)
        {
            this.Folder = folder;
            this.HandlerSet = uSync.Handlers.DefaultSet;
        }
    }
}
