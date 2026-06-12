using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Folders")]
public class SyncFolderController : uSyncControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ISyncConfigService _configService;
    private readonly ISyncHandlerFactory _handlerFactory;

    public SyncFolderController(ISyncService syncService, ISyncConfigService configService, ISyncHandlerFactory handlerFactory)
    {
        _syncService = syncService;
        _configService = configService;
        _handlerFactory = handlerFactory;
    }

    [HttpPost("MergeExport")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<IActionResult> MergeExportFolder()
    {
        var folders = _configService.GetFolders();
        var handlers = _handlerFactory.GetValidHandlers(new BackOffice.SyncHandlers.Models.SyncHandlerOptions { Set = _configService.Settings.DefaultSet });
        var result = await _syncService.MergeExportFolder(folders, handlers);
        return Ok(result);
    }
}
