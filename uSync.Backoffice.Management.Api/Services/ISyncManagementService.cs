using Umbraco.Cms.Core.Models.Membership;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;
using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Backoffice.Management.Api.Services;
public interface ISyncManagementService
{
    Stream CompressExportFolder();
    List<SyncActionGroup> GetActions();
    Func<SyncActionOptions, uSyncCallbacks, Task<SyncActionResult>> GetHandlerMethodAsync(HandlerActions action);
    
    [Obsolete("Pass in IUser for better logging, will be removed in v16")]
    Task<PerformActionResponse> PerformActionAsync(PerformActionRequest actionRequest)
        => PerformActionAsync(actionRequest, null);

    Task<PerformActionResponse> PerformActionAsync(PerformActionRequest actionRequest, IUser? user);
    UploadImportResult UnpackStream(Stream stream);
}