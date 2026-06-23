using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Core;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Filters;

using uSync.Backoffice.Management.Api.Configuration;
using uSync.BackOffice.Authorization;

namespace uSync.Backoffice.Management.Api.Controllers;

[ApiController]
[uSyncVersionedRoute("")]
// [Authorize(Policy = SyncAuthorizationPolicies.TreeAccessuSync)]
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
[MapToApi(uSyncClient.Api.ApiName)]
// [DisableBrowserCache]
[JsonOptionsName(Umbraco.Cms.Core.Constants.JsonOptionsNames.BackOffice)]
public class uSyncControllerBase : ControllerBase
{
}
