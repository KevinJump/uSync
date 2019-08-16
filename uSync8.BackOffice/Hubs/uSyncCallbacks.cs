using uSync8.BackOffice.SyncHandlers;
using static uSync8.BackOffice.uSyncService;

namespace uSync8.BackOffice
{
    public class uSyncCallbacks
    {
        public SyncEventCallback Callback { get; private set; }
        public SyncUpdateCallback Update { get; private set; }

        public uSyncCallbacks(SyncEventCallback callback, SyncUpdateCallback update)
        {
            this.Callback = callback;
            this.Update = update;
        }
    }
}
