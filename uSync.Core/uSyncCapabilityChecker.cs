using Umbraco.Cms.Core.Configuration;

namespace uSync.Core;

/// <summary>
///  a centralized way of telling if the current version of umbraco has 
///  certain features or not. 
/// </summary>
public class uSyncCapabilityChecker
{
    private readonly IUmbracoVersion _version;
    public uSyncCapabilityChecker(IUmbracoVersion version)
    {
        _version = version;
    }

    /// <summary>
    ///  History cleanup was introduced in Umbraco 9.1 
    /// </summary>
    /// <remarks>
    ///  anything above v9.1 has history cleanup.
    /// </remarks>
    public bool HasHistoryCleanup
        => _version.Version.Major != 9 || _version.Version.Minor >= 1;

    /// <summary>
    ///  Has a runtime mode introduced in v10.1 
    /// </summary>
    /// <remarks>
    ///  Runtime mode of Production means you can't update views etc.
    /// </remarks>
    public bool HasRuntimeMode
        => _version.Version.Major > 10 ||
        _version.Version.Major == 10 && _version.Version.Minor > 1;


    /// <summary>
    ///  User groups has Language Permissions - introduced in Umbraco 10.2.0
    /// </summary>
    public bool HasGroupLanguagePermissions => _version.Version >= new Version(10, 2, 0);

    /// <summary>
    ///  Domains (Culture and Hostnames) can be sorted - introduced in v11.3.0
    /// </summary>
    public bool HasSortableDomains => _version.Version >= new Version(11, 3, 0);
}

