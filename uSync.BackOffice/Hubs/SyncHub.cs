using System;
using System.Globalization;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

namespace uSync.BackOffice.Hubs
{
    /// <summary>
    ///  SignalR Hub
    /// </summary>
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
