using uSync.BackOffice.Models;

namespace uSync.Backoffice.Management.Api.Models;

public class PerformActionResponse
{
    public required string RequestId { get; set; }

    public IEnumerable<SyncHandlerSummary>? Status { get; set; }
    public IEnumerable<uSyncActionView>? Actions { get; set; }

    public bool Complete { get; set; }

    /// <summary>
    ///  the action is being performed in the background
    /// </summary>
    public bool InBackground { get; set; }

    /// <summary>
    ///  id of the long-running operation, when <see cref="InBackground"/> is true.
    ///  used by the client to poll <c>Status</c> and reattach after a page reload.
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    ///  set when the request could not be started (e.g. a background run of this
    ///  action is already in progress). <see cref="Complete"/> will be true, but
    ///  no work has actually happened.
    /// </summary>
    public string? Message { get; set; }
}
