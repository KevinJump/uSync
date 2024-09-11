using System.Diagnostics;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Core.Semver;

using Umbraco.Extensions;

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

    private string GetuSyncVersion()
    {
        var assembly = typeof(uSyncClient).Assembly;
        try
        {
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.GetAssemblyFile().FullName);
            var productVersion = SemVersion.Parse(fileVersionInfo.ProductVersion ?? assembly.GetName()?.Version?.ToString(3) ?? "14.2.0");
            return productVersion.ToSemanticStringWithoutBuild();
        }
        catch
        {
            return assembly.GetName()?.Version?.ToString(3) ?? "14.0.0";
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