using Microsoft.AspNetCore.Authorization;

using System.Threading.Tasks;
using Umbraco.Cms.Api.Management.Security.Authorization;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Extensions;

namespace uSync.BackOffice.Authorization;

/// <summary>
/// Security policy constants used in Umbraco by uSync
/// </summary>
public static class SyncAuthorizationPolicies
{
    /// <summary>
    ///  name of the uSyncTreeAccess policy.
    /// </summary>
    public const string TreeAccessuSync = nameof(TreeAccessuSync);
}

/// <summary>
///  this is identical to the internal AllowedApplicationRequirement, but because
///  that is internal, we have to replicate all the code. 
/// </summary>
public sealed class uSyncApplicationRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///  list of applications that this requirement will check against. 
    /// </summary>
    public string[] Applications { get; }

    /// <summary>
    ///  create a new requirement for the given applications
    /// </summary>
    /// <param name="applications"></param>
    public uSyncApplicationRequirement(params string[] applications)
    {
        Applications = applications;
    }
}

/// <summary>
///  public version of internal Umbraco AllowedApplicationHandler - so we can secure to a tree. 
/// </summary>
public sealed class uSyncAllowedApplicationHandler : MustSatisfyRequirementAuthorizationHandler<uSyncApplicationRequirement>
{
    private readonly IAuthorizationHelper _authorizationHelper;

    /// <summary>
    ///  new handler for the given authorization helper
    /// </summary>
    public uSyncAllowedApplicationHandler(IAuthorizationHelper authorizationHelper)
        => _authorizationHelper = authorizationHelper;

    /// <summary>
    ///   check to see if this is authorized
    /// </summary>
    protected override Task<bool> IsAuthorized(AuthorizationHandlerContext context, uSyncApplicationRequirement requirement)
    {
        var allowed = _authorizationHelper.TryGetUmbracoUser(context.User, out IUser? user)
                      && user.AllowedSections.ContainsAny(requirement.Applications);
        return Task.FromResult(allowed);
    }
}
