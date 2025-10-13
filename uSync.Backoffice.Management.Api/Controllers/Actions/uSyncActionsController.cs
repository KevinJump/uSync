using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using uSync.Backoffice.Management.Api.Models;
using uSync.Backoffice.Management.Api.Services;

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

    [HttpGet("Actions")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(List<SyncActionGroup>), 200)]
    [Obsolete("This endpoint is deprecated, use ActionsBySet instead. will be removed in v18")]
    public async Task<List<SyncActionGroup>> GetActions()
    {
        return await Task.FromResult(_syncManagementService.GetActions());
    }

    [HttpGet("ActionsBySet")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(List<SyncActionGroup>), 200)]
    public async Task<List<SyncActionGroup>> GetActionsBySet(string setName)
    {
        return await Task.FromResult(_syncManagementService.GetActions(setName));
    }   


}
