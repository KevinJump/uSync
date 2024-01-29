using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;

namespace uSync.Backoffice.Management.Api.Controllers;

[ApiController]
[VersionedApiBackOfficeRoute("uSync")]
// [Authorize(Policy = "New" + AuthorizationPolicies.BackOfficeAccess)]
[MapToApi("uSync")]
[JsonOptionsName("uSync")]
[ApiExplorerSettings(GroupName = "uSync")]
public class uSyncControllerBase
{
}
