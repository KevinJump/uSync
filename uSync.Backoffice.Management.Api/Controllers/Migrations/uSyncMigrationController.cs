using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using uSync.BackOffice.Configuration;
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
    public async Task<SyncLegacyCheckResponse> CheckLegacy()
    {

        List<string> types = [];
        if (_legacyService.TryGetLatestLegacyFolder(out var folder) && folder is not null)
        {
            types = await _legacyService.FindLegacyDataTypesAsync(folder);
        }

        return new SyncLegacyCheckResponse
        {
            HasLegacy = folder != null,
            LegacyFolder = folder,
            LegacyTypes = [.. types.Distinct()],
        };
    }

    [HttpPost("IgnoreLegacy")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<bool> IgnoreLegacy()
	{
		if (_legacyService.TryGetLatestLegacyFolder(out var folder) && folder is not null)
		{
			await _legacyService.IgnoreLegacyFolderAsync(folder, "folder will not show up as legacy.");
			return true;
		}

		return false;
	}

    [HttpPost("CopyLegacy")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public async Task<bool> CopyLegacy()
	{
		if (_legacyService.TryGetLatestLegacyFolder(out var folder) && folder is not null)
		{
			_legacyService.CopyLegacyFolder(folder);
            await _legacyService.IgnoreLegacyFolderAsync(folder, $"folder has been copied to v{uSync.BackOffice.uSync.Version.Major} as latest");
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

    public string LatestFolder { get; set; } = $"uSync/{uSync.BackOffice.uSync.Version.Major}";
    public string LatestVersion { get; set; } = uSync.BackOffice.uSync.Version.Major.ToString();

}
