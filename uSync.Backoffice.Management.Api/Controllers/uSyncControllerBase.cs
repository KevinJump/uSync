using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Core;
using Umbraco.Cms.Web.Common.Authorization;

using uSync.Backoffice.Management.Api.Configuration;

namespace uSync.Backoffice.Management.Api.Controllers;

[ApiController]
[uSyncVersionedRoute("")]
[Authorize(Policy = "New" + AuthorizationPolicies.BackOfficeAccess)]
[MapToApi(uSyncClient.Api.ApiName)]
// [JsonOptionsName(uSyncClient.Api.ApiName)]
[JsonOptionsName(Constants.JsonOptionsNames.BackOffice)]

public class uSyncControllerBase
{
}
