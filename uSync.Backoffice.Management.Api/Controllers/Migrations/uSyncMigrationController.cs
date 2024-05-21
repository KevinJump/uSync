using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using uSync.BackOffice.Legacy;

namespace uSync.Backoffice.Management.Api.Controllers.Migrations;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Migrations")]
public class uSyncMigrationController : uSyncControllerBase
{
    private readonly ISyncLegacyService _legacyService;

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

    [HttpPost("IgnoreLegacy")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public bool IgnoreLegacy()
	{
		if (_legacyService.TryGetLatestLegacyFolder(out var folder) && folder is not null)
		{
			_legacyService.IgnoreLegacyFolder(folder, "folder will not showup as legacy.");
			return true;
		}

		return false;
	}

    [HttpPost("CopyLegacy")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public bool CopyLegacy()
	{
		if (_legacyService.TryGetLatestLegacyFolder(out var folder) && folder is not null)
		{
			_legacyService.CopyLegacyFolder(folder);
            _legacyService.IgnoreLegacyFolder(folder, "folder has been copied to v14 as latest");
			return true;
		}

		return false;
	}
}

public class SyncLegacyCheckResponse
{
    public bool HasLegacy { get; set; }

    public string? LegacyFolder { get; set; }

    public string[] LegacyTypes { get; set; } = [];
}
