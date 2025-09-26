using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using System;
using System.Globalization;
using System.Threading.Tasks;

using Umbraco.Cms.Web.Common.Authorization;

namespace uSync.BackOffice.Hubs
{
    /// <summary>
    ///  SignalR Hub
    /// </summary>
    [Authorize(Policy = uSyncHubPolicy.AccessHub)]
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
    ///  Iterface for the ISyncHub
    /// </summary>
    public interface ISyncHub
    {
        /// <summary>
        ///  refresh the hub
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task refreshed(int id);
    }
}
