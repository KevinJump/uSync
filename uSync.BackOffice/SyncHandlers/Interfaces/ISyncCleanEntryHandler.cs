using System.Collections.Generic;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.SyncHandlers.Interfaces
{
    public interface ISyncCleanEntryHandler
    {
        IEnumerable<uSyncAction> ProcessCleanActions(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config);
    }
}
