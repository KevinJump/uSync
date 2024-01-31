using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Web.Common.Authorization;

using uSync.Backoffice.Management.Api.Configuration;

namespace uSync.Backoffice.Management.Api.Controllers;

[ApiController]
[uSyncVersionedRoute("core")]
[Authorize(Policy = "New" + AuthorizationPolicies.BackOfficeAccess)]
[MapToApi(uSyncClient.Api.ApiName)]
[JsonOptionsName(uSyncClient.Api.ApiName)]
public class uSyncControllerBase
{
}
