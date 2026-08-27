using Umbraco.Cms.Core.Models.Membership;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
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
    Task<SyncFileVersionCheckResult> GetSyncFileInfo();
    Task<PerformActionResponse> PerformActionAsync(PerformActionRequest actionRequest, IUser? user);

    [Obsolete("use UnpackStreamAsync will be removed in v19")]
    UploadImportResult UnpackStream(Stream stream)
        => UnpackStreamAsync(stream).Result;
    Task<UploadImportResult> UnpackStreamAsync(Stream stream);

    /// <summary>
    ///  get the status of a background operation, so the client can poll for
    ///  progress or reattach to a run after a page reload.
    /// </summary>
    /// <remarks>
    ///  Added after the initial release of this interface. Default implementation
    ///  reports "not supported" rather than throwing, so an existing implementation
    ///  of <see cref="ISyncManagementService"/> that predates background processing
    ///  keeps compiling and behaves sensibly (the client just sees a completed,
    ///  unsupported status instead of a 500) without needing to implement this.
    /// </remarks>
    Task<SyncOperationStatusResponse> GetOperationStatusAsync(Guid operationId)
        => Task.FromResult(new SyncOperationStatusResponse
        {
            OperationId = operationId.ToString(),
            OperationStatus = "NotSupported",
            Complete = true,
            Message = "This ISyncManagementService implementation does not support background operation status."
        });

    /// <summary>
    ///  get the run (if any) currently active on the server.
    /// </summary>
    /// <remarks>
    ///  See the remarks on <see cref="GetOperationStatusAsync(Guid)"/> - the default
    ///  reports nothing running, which is always a safe answer for an implementation
    ///  that doesn't track background runs.
    /// </remarks>
    Task<SyncRunningOperationResponse> GetRunningOperationAsync()
        => Task.FromResult(new SyncRunningOperationResponse());
}