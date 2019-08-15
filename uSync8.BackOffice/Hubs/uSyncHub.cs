using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.Hubs
{
    public class uSyncHub : Hub
    {
        public string GetTime()
            => DateTime.Now.ToString();
    }
}
