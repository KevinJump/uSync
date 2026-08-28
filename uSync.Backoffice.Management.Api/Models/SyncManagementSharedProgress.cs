using uSync.BackOffice.Models;

namespace uSync.Backoffice.Management.Api.Models;

/// <summary>
///  cross-server snapshot of a background run's progress, serialized into the
///  shared <c>umbracoLongRunningOperation.result</c> column so a status request
///  served by a different server (load-balanced backoffice) can still report it.
///  <para/>
///  deliberately a separate, versioned shape from <see cref="SyncManagementProgress"/> -
///  the wire format written here needs to stay readable by older/newer uSync
///  versions independently of the in-memory cache shape.
///  <para/>
///  interim writes (mid-run) omit <see cref="Actions"/> - only the handler
///  summaries are written on every step, to keep each write small. The full
///  action list is only included on the write that sets <see cref="Complete"/>.
/// </summary>
internal class SyncManagementSharedProgress
{
    public int Version { get; set; } = 1;

    public required Guid RequestId { get; set; }

    public string? Action { get; set; }

    public string? Message { get; set; }

    public bool Complete { get; set; }

    public IEnumerable<SyncHandlerSummary>? Status { get; set; }

    /// <summary>
    ///  populated only when <see cref="Complete"/> is true.
    /// </summary>
    public List<uSyncActionView>? Actions { get; set; }
}
