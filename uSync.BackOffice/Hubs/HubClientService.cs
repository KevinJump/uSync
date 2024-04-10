using System;

using Microsoft.AspNetCore.SignalR;

using uSync.BackOffice.Models;

namespace uSync.BackOffice.Hubs;

/// <summary>
/// Service to mange SignalR comms for uSync
/// </summary>
public class HubClientService
{
    private readonly IHubContext<SyncHub> _hubContext;
    private readonly string _clientId;

    /// <summary>
    /// Construct an new HubClientService (via DI)
    /// </summary>
    public HubClientService(IHubContext<SyncHub> hubContext, string clientId)
    {
        this._hubContext = hubContext;
        this._clientId = clientId;
    }

    /// <summary>
    /// Send an 'add' message to the client 
    /// </summary>
    public void SendMessage<TObject>(TObject item)
    {
        if (_hubContext != null && !string.IsNullOrWhiteSpace(_clientId))
        {
            var client = _hubContext.Clients.Client(_clientId);
            if (client != null)
            {
                client.SendAsync("Add", item).Wait();
                return;
            }

            _hubContext.Clients.All.SendAsync("Add", item).Wait();
        }
    }

    /// <summary>
    /// Send an 'update' message to the client
    /// </summary>
    public void SendUpdate(Object message)
    {
        if (_hubContext == null || string.IsNullOrWhiteSpace(_clientId)) return;

        var client = _hubContext.Clients.Client(_clientId);
        if (client == null) return;
        
        client.SendAsync("Update", message).Wait();
        return;       
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
    public uSyncCallbacks Callbacks() => new(this.PostSummary, this.PostUpdate);
}
