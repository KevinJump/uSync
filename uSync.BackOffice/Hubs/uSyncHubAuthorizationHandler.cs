using Microsoft.AspNetCore.Authorization;

using System.Threading.Tasks;

using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.BackOffice.Authorization;

namespace uSync.BackOffice.Hubs;

public class uSyncHubAuthorizationHandler : MustSatisfyRequirementAuthorizationHandler<uSyncHubRequirement>
{
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    public uSyncHubAuthorizationHandler(IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    protected override Task<bool> IsAuthorized(AuthorizationHandlerContext context, uSyncHubRequirement requirement)
    {
        var current = _backOfficeSecurityAccessor.BackOfficeSecurity.CurrentUser;
        return Task.FromResult(current != null);
    }
}

public class uSyncHubRequirement : IAuthorizationRequirement
{ 
}

public static class uSyncHubPolicy
{
    public const string AccessHub = "uSyncHubPolicy";
}