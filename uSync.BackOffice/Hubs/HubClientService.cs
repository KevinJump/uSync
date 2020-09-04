using System;

namespace uSync.BackOffice.Hubs
{
    public class HubClientService
    {
        // private readonly IHubContext hubContext;
        private readonly string clientId;

        public HubClientService(string clientId)
        {
            // hubContext = GlobalHost.ConnectionManager.GetHubContext<uSyncHub>();
            this.clientId = clientId;
        }

        public void SendMessage<TObject>(TObject item)
        {
            /*
            if (hubContext != null && !string.IsNullOrWhiteSpace(clientId))
            {
                var client = hubContext.Clients.Client(clientId);
                if (client != null)
                {
                    client.Add(item);
                    return;
                }

                hubContext.Clients.All.Add(item);
            }
            */
        }

        public void SendUpdate(Object message)
        {
            /*
            if (hubContext != null && !string.IsNullOrWhiteSpace(clientId))
            {
                var client = hubContext.Clients.Client(clientId);
                if (client != null)
                {
                    client.Update(message);
                    return;
                }
                hubContext.Clients.All.Update(message);
            }
            */
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
