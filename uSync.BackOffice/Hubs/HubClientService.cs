using System;

using Microsoft.AspNetCore.SignalR;

using uSync.BackOffice.Models;

namespace uSync.BackOffice.Hubs;

/// <summary>
/// Service to mange SignalR comms for uSync
/// </summary>
public class HubClientService
{
    private readonly IHubContext<SyncHub> hubContext;
    private readonly string clientId;

    /// <summary>
    /// Construct an new HubClientService (via DI)
    /// </summary>
    public HubClientService(IHubContext<SyncHub> hubContext, string clientId)
    {
        this.hubContext = hubContext;
        this.clientId = clientId;
    }

    /// <summary>
    /// Send an 'add' message to the client 
    /// </summary>
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

    /// <summary>
    /// Send an 'update' message to the client
    /// </summary>
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

    /// <summary>
    ///  post a summary 'add' message to the client
    /// </summary>
    public void PostSummary(SyncProgressSummary summary)
    {
        this.SendMessage(summary);
    }

    /// <summary>
    ///  post a progress 'update' message to the client 
    /// </summary>
    public void PostUpdate(string message, int count, int total)
    {
        this.SendUpdate(new uSyncUpdateMessage()
        {
            Message = message,
            Count = count,
            Total = total
        });
    }

    /// <summary>
    ///  get the uSync callbacks for this connection
    /// </summary>
    /// <returns></returns>
    public uSyncCallbacks Callbacks() =>
        new uSyncCallbacks(this.PostSummary, this.PostUpdate);
}
