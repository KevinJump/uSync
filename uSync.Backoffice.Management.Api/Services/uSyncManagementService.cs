using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using uSync.Backoffice.Management.Api.Extensions;
using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Hubs;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;

namespace uSync.Backoffice.Management.Api.Services;

/// <summary>
///  the thing that does the imports, reports, and exports. 
/// </summary>
internal class uSyncManagementService : ISyncManagementService
{
    private readonly ISyncActionService _syncActionService;
    private readonly ISyncManagementCache _syncManagementCache;
    private readonly IHubContext<SyncHub> _hubContext;
    private readonly uSyncConfigService _configService;

    public uSyncManagementService(
        ISyncActionService syncActionService,
        uSyncConfigService configService,
        ISyncManagementCache syncManagementCache,
        IHubContext<SyncHub> hubContext)
    {
        _syncActionService = syncActionService;
        _configService = configService;
        _syncManagementCache = syncManagementCache;
        _hubContext = hubContext;
    }

    public PerformActionResponse PerformAction(PerformActionRequest actionRequest)
    {
        if (Enum.TryParse(actionRequest.Action, out HandlerActions action) is false)
            throw new ArgumentException($"Invalid action {actionRequest.Action}");

        var handlers = _syncActionService.GetActionHandlers(action, actionRequest.Options)
            .ToList();

        Guid requestId = GetRequestId(actionRequest);

        if (actionRequest.StepNumber >= handlers.Count)
        {
            var finalActions = _syncManagementCache.GetCachedActions(requestId);
            // finished. 
            return new PerformActionResponse
            {
                RequestId = requestId.ToString(),
                Actions = finalActions.Select(x => x.ToActionView()),
                Complete = true,
                Status = GetSummaries(handlers, actionRequest.StepNumber)
            };
        }

        var currentHandler = handlers[actionRequest.StepNumber];
        var method = GetHandlerMethod(action);

        HubClientService? hubClient = default;
        if (actionRequest.Options?.ClientId != null)
        {
            hubClient = new HubClientService(_hubContext, actionRequest.Options.ClientId);
        }

        var handlerOptions = new SyncActionOptions()
        {
            Folders = _configService.GetFolders(),
            Set = actionRequest.Options?.Set ?? _configService.Settings.DefaultSet,
            Force = actionRequest.Options?.Force ?? false,
            Actions = new List<uSyncAction>(),
            Handler = currentHandler.Alias
        };

        uSyncCallbacks callbacks = hubClient?.Callbacks() ?? new uSyncCallbacks(null, null);

        var results = method(handlerOptions, callbacks);
        _syncManagementCache.CacheItems(requestId, results.Actions, false);

        return new PerformActionResponse
        {
            RequestId = requestId.ToString(),
            Actions = results.Actions.Select(x => x.ToActionView()),
            Status = GetSummaries(handlers, actionRequest.StepNumber),
            Complete = false
        };
    }

    private Guid GetRequestId(PerformActionRequest actionRequest)
    {
        Guid requestId;
        if (string.IsNullOrEmpty(actionRequest.RequestId))
            requestId = _syncManagementCache.GetNewCacheId();
        else if (Guid.TryParse(actionRequest.RequestId, out requestId) is false)
            throw new ArgumentException("requestId invalid format");
        return requestId;
    }

    private IEnumerable<SyncHandlerSummary> GetSummaries(List<SyncHandlerView> handlers, int step)
    {
        for (int n = 0; n < handlers.Count; n++)
        {
            yield return new SyncHandlerSummary
            {
                Name = handlers[n].Name,
                Icon = handlers[n].Icon,
                Status = n < step ? HandlerStatus.Complete :
                     n == step ? HandlerStatus.Processing : HandlerStatus.Pending
            };
        }
    }


    public Func<SyncActionOptions, uSyncCallbacks, SyncActionResult> GetHandlerMethod(HandlerActions action)
    {
        return action switch
        {
            HandlerActions.Import => _syncActionService.ImportHandler,
            HandlerActions.Report => _syncActionService.ReportHandler,
            HandlerActions.Export => _syncActionService.ExportHandler,
            _ => throw new InvalidOperationException($"Unknown method {action}"),
        };
    }

}
