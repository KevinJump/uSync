using System.Collections.Generic;
using System.Linq;

using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Notifications
{
    public class uSyncEventHandler
    {
        private SyncHandlerCollection syncHandlers;

        public uSyncEventHandler(SyncHandlerCollection syncHandlers)
        {
            this.syncHandlers = syncHandlers;
        }

        private void HandleSave<T>(IEnumerable<T> savedEntities)
        {
            foreach (var handler in syncHandlers.Where(x => x.ItemType == typeof(T).ToString()))
            {
                // call the save.
            }
        }
    }
}
