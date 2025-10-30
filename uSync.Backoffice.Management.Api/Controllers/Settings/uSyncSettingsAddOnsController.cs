using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using System.Diagnostics;

using Umbraco.Cms.Core.Semver;
using Umbraco.Extensions;

using uSync.BackOffice.Extensions;

namespace uSync.Backoffice.Management.Api.Controllers.Settings;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Settings")]
public class uSyncSettingsAddOnsController : uSyncControllerBase
{
    [HttpGet("AddOnSplash")]
    [ProducesResponseType(typeof(uSyncAddonSplash), 200)]
    public uSyncAddonSplash GetAddonSplash()
        => new uSyncAddonSplash();

    [HttpGet("AddOns")]
    [ProducesResponseType(typeof(uSyncAddonInfo), 200)]
    public uSyncAddonInfo GetAddOns()
    {
        return new uSyncAddonInfo
        {
            Version = GetuSyncVersion(),
        };
    }

    private static string GetuSyncVersion()
    {
        var assembly = typeof(uSyncClient).Assembly;
        try
        {
            return assembly.GetAssemblyProductVersion().ToSemanticStringWithoutBuild();
        }
        catch
        {
            return assembly.GetName()?.Version?.ToString(3) ?? "17.x";
        }
    }
}

public class uSyncAddonInfo
{
    public required string Version { get; set; }
}

public class uSyncAddonSplash
{

}