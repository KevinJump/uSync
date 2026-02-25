using Umbraco.Cms.Core.Models.Membership;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;
using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Backoffice.Management.Api.Services;
public interface ISyncManagementService
{
    Stream CompressExportFolder();

    [Obsolete("Use GetActions(string setName) instead, this will be removed in v18")]
    List<SyncActionGroup> GetActions();

    [Obsolete("Use GetActionsAsync(string setName) instead, this will be removed in v19")]
    List<SyncActionGroup> GetActions(string setName);
    
    Task<List<SyncActionGroup>> GetActionsAsync(string setName);
    
    Func<SyncActionOptions, uSyncCallbacks, Task<SyncActionResult>> GetHandlerMethodAsync(HandlerActions action);
    
    Task<PerformActionResponse> PerformActionAsync(PerformActionRequest actionRequest, IUser? user);
    UploadImportResult UnpackStream(Stream stream);
}