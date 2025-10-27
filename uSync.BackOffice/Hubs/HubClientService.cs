using Microsoft.AspNetCore.SignalR;

using System;
using System.Collections.Generic;

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

    public void PostComplete(Guid requestId, string message, bool success, IEnumerable<uSyncActionView> actions)
    {
        if (_hubContext == null || string.IsNullOrWhiteSpace(_clientId)) return;

        var client = _hubContext.Clients.Client(_clientId);
        if (client == null) return;

        client.SendAsync("Complete", new SyncCompleteMessage
        {
            RequestId = requestId,
            Message = message,
            Success = success,
            Actions = actions
        }).Wait();
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
    public uSyncCallbacks Callbacks() => new(this.PostSummary, this.PostUpdate, this.SetCountRange, this.PostIncrementalUpdate, this.PostComplete);

    private int _start = 0;
    private int _end = 0;

    /// <summary>
    ///  set a range (start to end) that we expect the next set of updates to be bound within.
    /// </summary>
    public void SetCountRange(int start, int end)
    {
        _start = start;
        _end = end;
    }

    /// <summary>
    ///  post an update and increment the counter by one.
    /// </summary>
    public void PostIncrementalUpdate(string message)
    {
        _start++;
        if (_start > _end) _end = _start;
        this.PostUpdate(message, _start, _end);
    }
}
