using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using uSync.BackOffice.Legacy;
using uSync.BackOffice.Legacy.Migrators;

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
        List<SyncLegacyMigrationInfo> migrations = [];
        if (_legacyService.TryGetLatestLegacyFolder(out var folder) && folder is not null)
        {
            types = await _legacyService.FindLegacyDataTypesAsync(folder);
            migrations = await _legacyService.FindMigrationsAsync(folder);
        }

        return new SyncLegacyCheckResponse
        {
            HasLegacy = folder != null,
            LegacyFolder = folder,
            LegacyTypes = [.. types.Distinct()],
            Migrations = [.. migrations.Select(x => new SyncLegacyMigrationResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Notice = x.Notice,
                Count = x.Count,
            })],
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
    public async Task<bool> CopyLegacy([FromBody] SyncLegacyCopyRequest? request = null)
    {
        if (_legacyService.TryGetLatestLegacyFolder(out var folder) && folder is not null)
        {
            await _legacyService.CopyLegacyFolderAsync(folder, request?.Migrations ?? []);
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

    public SyncLegacyMigrationResponse[] Migrations { get; set; } = [];

    public string LatestFolder { get; set; } = $"uSync/{uSync.BackOffice.uSync.Version.Major}";
    public string LatestVersion { get; set; } = uSync.BackOffice.uSync.Version.Major.ToString();

}

public class SyncLegacyMigrationResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Notice { get; set; }

    /// <summary>
    ///  number of data types the migration will change.
    /// </summary>
    public int Count { get; set; }
}

public class SyncLegacyCopyRequest
{
    /// <summary>
    ///  ids of the migrations to run as the folder is copied.
    /// </summary>
    public string[] Migrations { get; set; } = [];
}
