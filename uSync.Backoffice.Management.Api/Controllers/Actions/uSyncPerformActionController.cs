using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using uSync.Backoffice.Management.Api.Models;
using uSync.Backoffice.Management.Api.Services;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class uSyncPerformActionController : uSyncControllerBase
{
    private readonly ISyncManagementService _managementService;

    public uSyncPerformActionController(ISyncManagementService managementService)
    {
        _managementService = managementService;
    }

    [HttpPost("Perform")]
    [ProducesResponseType(typeof(PerformActionResponse), 200)]
    public async Task<PerformActionResponse> PerformAction(PerformActionRequest model)
        => await _managementService.PerformActionAsync(model);
}
