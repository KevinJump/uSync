using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;
using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Backoffice.Management.Api.Services;
public interface ISyncManagementService
{
	List<SyncActionGroup> GetActions();
	Func<SyncActionOptions, uSyncCallbacks, SyncActionResult> GetHandlerMethod(HandlerActions action);
    PerformActionResponse PerformAction(PerformActionRequest actionRequest);
}