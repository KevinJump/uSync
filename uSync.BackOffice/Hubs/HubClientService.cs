using System;

using Microsoft.AspNetCore.SignalR;

namespace uSync.BackOffice.Hubs
{
    public class HubClientService
    {
        private readonly IHubContext<SyncHub> hubContext;
        private readonly string clientId;

        public HubClientService(IHubContext<SyncHub> hubContext, string clientId)
        {
            this.hubContext = hubContext;
            this.clientId = clientId;
        }

        public void SendMessage<TObject>(TObject item)
        {
            if (hubContext != null && !string.IsNullOrWhiteSpace(clientId))
            {
                var client = hubContext.Clients.Client(clientId);
                if (client != null)
                {
                    client.SendAsync("Add", item).Wait();
                    return;
                }

                hubContext.Clients.All.SendAsync("Add", item).Wait();
            }
        }

        public void SendUpdate(Object message)
        {
            if (hubContext != null && !string.IsNullOrWhiteSpace(clientId))
            {
                var client = hubContext.Clients.Client(clientId);
                if (client != null)
                {
                    client.SendAsync("Update", message).Wait();
                    return;
                }
                hubContext.Clients.All.SendAsync("Update", message).Wait();
            }
        }


        public void PostSummary(SyncProgressSummary summary)
        {
            this.SendMessage(summary);
        }

        public void PostUpdate(string message, int count, int total)
        {
            this.SendUpdate(new uSyncUpdateMessage()
            {
                Message = message,
                Count = count,
                Total = total
            });
        }

        public uSyncCallbacks Callbacks() =>
            new uSyncCallbacks(this.PostSummary, this.PostUpdate);
    }
}
