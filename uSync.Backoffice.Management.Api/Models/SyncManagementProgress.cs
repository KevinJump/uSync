using uSync.BackOffice.Models;

namespace uSync.Backoffice.Management.Api.Models;

/// <summary>
///  a snapshot of an in-progress (or just completed) run, cached so it can be
///  polled by the status endpoint - this is what lets the UI reattach to a
///  background run after a page reload.
/// </summary>
internal class SyncManagementProgress
{
    public required Guid RequestId { get; set; }

    public Guid? OperationId { get; set; }

    public required string Action { get; set; }

    public IEnumerable<SyncHandlerSummary> Status { get; set; } = [];

    public IEnumerable<uSyncActionView> Actions { get; set; } = [];

    public bool Complete { get; set; }

    public string? Message { get; set; }

    public DateTime LastUpdated { get; set; }
}
