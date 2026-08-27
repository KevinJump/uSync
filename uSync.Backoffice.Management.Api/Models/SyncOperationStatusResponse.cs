using uSync.BackOffice.Models;

namespace uSync.Backoffice.Management.Api.Models;

/// <summary>
///  status of a background sync operation, polled by the client so it can
///  show progress and reattach to a run it did not start (e.g. after reload).
/// </summary>
public class SyncOperationStatusResponse
{
    public required string OperationId { get; set; }

    public string? RequestId { get; set; }

    public string? Action { get; set; }

    /// <summary>
    ///  the underlying long-running-operation status (Enqueued, Running, Success, Failed, Stale, NotFound)
    /// </summary>
    public required string OperationStatus { get; set; }

    public bool Complete { get; set; }

    public IEnumerable<SyncHandlerSummary>? Status { get; set; }

    public IEnumerable<uSyncActionView>? Actions { get; set; }

    public string? Message { get; set; }
}

/// <summary>
///  the run (if any) currently active on the server - lets the client
///  discover and reattach to a background run it did not start itself.
/// </summary>
public class SyncRunningOperationResponse
{
    public string? OperationId { get; set; }

    public string? RequestId { get; set; }

    public string? Action { get; set; }
}
