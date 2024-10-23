using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.SyncHandlers.Interfaces;

/// <summary>
/// Interface for handlers that can process cleaning the folder with _clean files 
/// </summary>
public interface ISyncCleanEntryHandler
{
    /// <summary>
    /// process any clean actions that have been identified during the import 
    /// </summary>
    [Obsolete("Use ProcessCleanActionsAsync instead will be removed in v16")]
    IEnumerable<uSyncAction> ProcessCleanActions(string? folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
        => ProcessCleanActionsAsync(folder, actions, config).Result;

    Task<IEnumerable<uSyncAction>> ProcessCleanActionsAsync(string? folder, IEnumerable<uSyncAction> actions, HandlerSettings config);
}
