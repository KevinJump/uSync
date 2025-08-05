using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using uSync.BackOffice.Configuration;

namespace uSync.Backoffice.Management.Api.Controllers.Settings;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Settings")]
public class uSyncSettingsController : uSyncControllerBase
{
    private readonly ISyncConfigService _configService;
    private readonly IConfiguration _configuration;

    public uSyncSettingsController(ISyncConfigService configService, IConfiguration configuration)
    {
        _configService = configService;
        _configuration = configuration;
    }

    [HttpGet("Settings")]
    [ProducesResponseType(typeof(uSyncSettings), 200)]
    public uSyncSettings GetSettings()
    {
        var settings = _configService.Settings;
        settings.Folders = _configService.GetFolders();
        return settings;
    }

    [HttpGet("HandlerSettings")]
    [ProducesResponseType(typeof(uSyncHandlerSetSettings), 200)]
    public uSyncHandlerSetSettings GetHandlerSetSettings(string id)
        => _configService.GetSetSettings(id);

    [HttpGet("Sets")]
    [ProducesResponseType<List<SyncSelectableSet>>(StatusCodes.Status200OK)]
    public IActionResult GetSets()
    {
        var section = _configuration.GetSection(
            BackOffice.uSync.Configuration.uSyncSetsConfig);

        var children = section.GetChildren();
        if (children is null || !children.Any())
        {
            var defaultSet = new SyncSelectableSet
            {
                Name = BackOffice.uSync.Sets.DefaultSet,
                Settings = _configService.GetSetSettings(BackOffice.uSync.Sets.DefaultSet)
            };

            return Ok(new List<SyncSelectableSet>() { defaultSet });
        }

        List<SyncSelectableSet> sets = new();

        foreach (var item in section.GetChildren())
        {
            var settings = _configService.GetSetSettings(item.Key);
            if (settings is null || settings.Enabled is false) continue;
            sets.Add(new SyncSelectableSet
            {
                Name = item.Key,
                Settings = settings
            });
        }

        return Ok(sets);
    }
}

public class SyncSelectableSet
{
    public required string Name { get; init; } 
    public required uSyncHandlerSetSettings Settings { get; init; }
}
