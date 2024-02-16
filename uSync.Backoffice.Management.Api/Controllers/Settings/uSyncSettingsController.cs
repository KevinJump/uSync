using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using uSync.BackOffice.Configuration;

namespace uSync.Backoffice.Management.Api.Controllers.Settings;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Settings")]
public class uSyncSettingsController : uSyncControllerBase
{
    private readonly uSyncConfigService _configService;

    public uSyncSettingsController(uSyncConfigService configService)
    {
        _configService = configService;
    }

    [HttpGet("Settings")]
    [ProducesResponseType(typeof(uSyncSettings), 200)]
    public uSyncSettings GetSettings()
        => _configService.Settings;

    [HttpGet("HandlerSettings")]
    [ProducesResponseType(typeof(uSyncHandlerSetSettings), 200)]
    public uSyncHandlerSetSettings GetHandlerSetSettings(string id)
        => _configService.GetSetSettings(id);
}
