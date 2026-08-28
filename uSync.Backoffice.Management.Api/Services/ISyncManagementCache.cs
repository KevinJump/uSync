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
    ///  when <see cref="SyncManagementProgress.OperationId"/> is set (background
    ///  runs only) this is also mirrored into the shared long-running-operation
    ///  record, so a status request served by a different server can see it.
    /// </summary>
    Task SaveProgressAsync(SyncManagementProgress progress);

    /// <summary>
    ///  get the last saved progress snapshot for a run (by uSync requestId).
    ///  only ever resolves from the local, in-process cache - use
    ///  <see cref="GetProgressForOperationAsync(Guid)"/> when the run may have
    ///  started on a different server.
    /// </summary>
    SyncManagementProgress? GetProgress(Guid requestId);

    /// <summary>
    ///  get the last saved progress for a background run by its long-running-operation
    ///  id, falling back to the shared record when this server did not run it
    ///  (or has since recycled) - the load-balanced-backoffice path.
    /// </summary>
    Task<SyncManagementProgress?> GetProgressForOperationAsync(Guid operationId);

    /// <summary>
    ///  register the long-running-operation id for a run, so the status endpoint
    ///  can go from the id the client holds back to the cached progress.
    /// </summary>
    void RegisterOperation(Guid operationId, Guid requestId);

    /// <summary>
    ///  look up the uSync requestId for a long-running-operation id.
    ///  only ever resolves from the local, in-process cache - use
    ///  <see cref="GetProgressForOperationAsync(Guid)"/> when the run may have
    ///  started on a different server.
    /// </summary>
    Guid? GetRequestIdForOperation(Guid operationId);
}
