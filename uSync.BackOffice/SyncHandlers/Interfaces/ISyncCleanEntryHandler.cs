using System.Collections.Generic;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.SyncHandlers.Interfaces
{
    /// <summary>
    /// Interface for handlers that can process cleaning the folder with _clean files 
    /// </summary>
    public interface ISyncCleanEntryHandler
    {
        /// <summary>
        /// process any clean actions that have been identified during the import 
        /// </summary>
        IEnumerable<uSyncAction> ProcessCleanActions(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config);
    }
}
