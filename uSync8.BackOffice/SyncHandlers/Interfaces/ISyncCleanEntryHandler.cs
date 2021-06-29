using System.Collections.Generic;

using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.SyncHandlers
{
    public interface ISyncCleanEntryHandler
    {
        IEnumerable<uSyncAction> ProcessCleanActions(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config);
    }
}
