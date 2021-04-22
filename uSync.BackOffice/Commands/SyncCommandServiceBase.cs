using System.IO;

namespace uSync.BackOffice.Commands
{
    public class SyncCommandServiceBase : SyncCommandBase
    {
        protected readonly uSyncService uSyncService;
        protected readonly uSyncCallbacks callbacks;

        public SyncCommandServiceBase(TextReader reader, TextWriter writer,
            uSyncService uSyncService) : base(reader, writer)
        {
            this.uSyncService = uSyncService;
            this.callbacks = new uSyncCallbacks(Summary, Update);

            AdvancedHelp = "\nOption param [folder]\tPath to the folder to use for operation\n";
        }

        // callbacks. 
        //  We can use the callbacks that are used for SignalR with a client
        //  to get progress reports on any actions.
        //  at the base level, we just remember the current step, and 
        //  write a '.' everytime we get something - we could go full 
        //  progress bar UI - but that depends 'at bit' being sure you are in 
        //  console mode and writer hasn't been pushed somewhere else. 
        private string currentStep = string.Empty;

        public void Summary(SyncProgressSummary summary)
        {
            currentStep = summary.Message;
        }

        public void Update(string message, int count, int total)
        {
            writer.Write(".");
        }

    }
}
