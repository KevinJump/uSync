using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;


using uSync.BackOffice;
using uSync.BackOffice.Models;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class SyncItemController : uSyncControllerBase
{
    private readonly ISyncService _syncService;

    public SyncItemController(ISyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpPost("Import")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(uSyncAction), 200)]
    public async Task<IActionResult> ImportSingle([FromBody] uSyncActionView action)
    {
        var result = await _syncService.ImportSingleItemAsync(action.Key, action.Handler);
        return Ok(result);
    }
}
