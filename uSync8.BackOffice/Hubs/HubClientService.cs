using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice.Hubs
{
    public class HubClientService
    {
        private readonly IHubContext hubContext;
        private readonly string clientId;

        public HubClientService(string clientId)
        {
            hubContext = GlobalHost.ConnectionManager.GetHubContext<uSyncHub>();
            this.clientId = clientId;
        }

        public void SendMessage<TObject>(TObject item)
        {
            if (hubContext != null)
            {
                if (!string.IsNullOrWhiteSpace(clientId))
                {
                    var client = hubContext.Clients.Client(clientId);
                    if (client != null)
                    {
                        client.Add(item);
                        return;
                    }
                }
                hubContext.Clients.All.Add(item);
            }
        }
    }
}
