using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;

namespace uSync.Backoffice.Management.Api.Services;
internal interface ISyncManagementCache
{
    void CacheItems(Guid id, IEnumerable<uSyncAction> actions, bool overwrite);
    List<uSyncAction> GetCachedActions(Guid id);
    Guid GetNewCacheId();
    bool IsValid(Guid id);

    void Clear(Guid id);

    /// <summary>
    ///  save the latest progress snapshot for a run, so it can be polled later.
    /// </summary>
    void SaveProgress(SyncManagementProgress progress);

    /// <summary>
    ///  get the last saved progress snapshot for a run (by uSync requestId).
    /// </summary>
    SyncManagementProgress? GetProgress(Guid requestId);

    /// <summary>
    ///  register the long-running-operation id for a run, so the status endpoint
    ///  can go from the id the client holds back to the cached progress.
    /// </summary>
    void RegisterOperation(Guid operationId, Guid requestId);

    /// <summary>
    ///  look up the uSync requestId for a long-running-operation id.
    /// </summary>
    Guid? GetRequestIdForOperation(Guid operationId);
}
