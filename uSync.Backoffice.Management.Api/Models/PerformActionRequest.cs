using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers;

namespace uSync.Backoffice.Management.Api.Models;

public class PerformActionRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string Action { get; set; } = HandlerActions.Report.ToString();
    public int StepNumber { get; set; }
    public uSyncOptions? Options { get; set; }
}
