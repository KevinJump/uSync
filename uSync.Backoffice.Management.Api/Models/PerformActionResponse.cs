using uSync.BackOffice.Models;

namespace uSync.Backoffice.Management.Api.Models;

public class PerformActionResponse
{
    public required string RequestId { get; set; }

    public IEnumerable<SyncHandlerSummary>? Status { get; set; }
    public IEnumerable<uSyncActionView>? Actions { get; set; }

    public bool Complete { get; set; }
}
