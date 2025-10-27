using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using System;
using System.Globalization;
using System.Threading.Tasks;

using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Core;
using Umbraco.Cms.Web.Common.Authorization;

namespace uSync.BackOffice.Hubs;

/// <summary>
///  SignalR Hub
/// </summary>
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
[JsonOptionsName(Constants.JsonOptionsNames.BackOffice)]
public class SyncHub : Hub<ISyncHub>
{
    /// <summary>
    ///  Get the current time 
    /// </summary>
    /// <remarks>
    /// Used to give the hub a purpose - not called 
    /// </remarks>
    public string GetTime()
        => DateTime.Now.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
///  Interface for the ISyncHub
/// </summary>
public interface ISyncHub
{
    /// <summary>
    ///  refresh the hub
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task Refreshed(int id);
}
