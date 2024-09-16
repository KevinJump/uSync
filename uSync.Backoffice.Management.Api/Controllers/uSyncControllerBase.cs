using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Core;

using uSync.Backoffice.Management.Api.Configuration;
using uSync.BackOffice.Authorization;

namespace uSync.Backoffice.Management.Api.Controllers;

[ApiController]
[uSyncVersionedRoute("")]
[Authorize(Policy = SyncAuthorizationPolicies.TreeAccessuSync)]
[MapToApi(uSyncClient.Api.ApiName)]
[JsonOptionsName(Constants.JsonOptionsNames.BackOffice)]
public class uSyncControllerBase
{
}
