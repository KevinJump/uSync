using System;

using Microsoft.AspNet.SignalR;

namespace uSync8.BackOffice.Hubs
{
    public class uSyncHub : Hub
    {
        public string GetTime()
            => DateTime.Now.ToString();
    }
}
