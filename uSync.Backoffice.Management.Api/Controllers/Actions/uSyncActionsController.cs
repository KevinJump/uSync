using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using uSync.Backoffice.Management.Api.Models;
using uSync.Backoffice.Management.Api.Services;
using uSync.BackOffice.Services;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class uSyncActionsController : uSyncControllerBase
{
    private readonly ISyncManagementService _syncManagementService;

    public uSyncActionsController(ISyncManagementService syncManagementService)
    {
        _syncManagementService = syncManagementService;
    }

    [HttpGet("ActionsBySet")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(List<SyncActionGroup>), 200)]
    public async Task<List<SyncActionGroup>> GetActionsBySet(string setName)
    {
        return await _syncManagementService.GetActionsAsync(setName);
    }

    [HttpGet("SyncFileInfo")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SyncFileVersionCheckResult), 200)]
    public async Task<SyncFileVersionCheckResult> GetSyncFileInfo()
        => await _syncManagementService.GetSyncFileInfo();

}
