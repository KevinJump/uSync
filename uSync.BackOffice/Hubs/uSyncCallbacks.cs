using uSync.BackOffice.SyncHandlers;

using static uSync.BackOffice.uSyncService;

namespace uSync.BackOffice
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
