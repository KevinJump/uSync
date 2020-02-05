using System.IO;

namespace uSync8.BackOffice.Commands
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
        }

        // callbacks. 
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
