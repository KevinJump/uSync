using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

namespace uSync.BackOffice.Hubs
{
    public class SyncHub : Hub<ISyncHub>
    {
        public string GetTime()
            => DateTime.Now.ToString();
    }

    public interface ISyncHub
    {
        Task refreshed(int id);
    }
}
