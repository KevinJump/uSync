using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using uSync.BackOffice.Legacy;

namespace uSync.Backoffice.Management.Api.Controllers.Migrations;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Migrations")]
public class uSyncMigrationController : uSyncControllerBase
{
    private ISyncLegacyService _legacyService;

    public uSyncMigrationController(ISyncLegacyService legacyService)
    {
        _legacyService = legacyService;
    }

    [HttpGet("CheckLegacy")]
    [ProducesResponseType<SyncLegacyCheckResponse>(200)]
    public SyncLegacyCheckResponse CheckLegacy()
    {

        List<string> types = [];
        if (_legacyService.TryGetLatestLegacyFolder(out var folder) && folder is not null)
        {
            types = _legacyService.FindLegacyDataTypes(folder);
        }

        return new SyncLegacyCheckResponse
        {
            HasLegacy = folder != null,
            LegacyFolder = folder,
            LegacyTypes = [.. types.Distinct()]
        };
    }
}

public class SyncLegacyCheckResponse
{
    public bool HasLegacy { get; set; }

    public string? LegacyFolder { get; set; }

    public string[] LegacyTypes { get; set; } = [];
}
