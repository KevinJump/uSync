using System.Reflection.Emit;

using Microsoft.AspNetCore.SignalR;

using Umbraco.Extensions;

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

    private readonly SyncHandlerFactory _handlerFactory;

    public uSyncManagementService(
        ISyncActionService syncActionService,
        uSyncConfigService configService,
        ISyncManagementCache syncManagementCache,
        IHubContext<SyncHub> hubContext,
        SyncHandlerFactory handlerFactory)
    {
        _syncActionService = syncActionService;
        _configService = configService;
        _syncManagementCache = syncManagementCache;
        _hubContext = hubContext;
        _handlerFactory = handlerFactory;
    }

    /// <summary>
    ///  Gets the list of available actions
    /// </summary>
    public List<SyncActionGroup> GetActions()
    {
        // TODO: Load the actions based on the handlers, and the config, (so they can be turned on and off)

        var defaultReport = new SyncActionButton()
        {
            Key = HandlerActions.Report.ToString(),
            Label = HandlerActions.Report.ToString(),
            Look = "secondary",
            Color = "positive"
        };

		var defaultImport = new SyncActionButton()
        {
            Key = HandlerActions.Import.ToString(),
            Label = HandlerActions.Import.ToString(),
            Look = "primary",
            Color = "positive",
            Children = [
                    new() {
                        Key = $"{HandlerActions.Import}",
                        Label= $"{HandlerActions.Import}Force",
                        Force = true
                    }
                ]
        };

		var defaultExport = new SyncActionButton()
		{
			Key = HandlerActions.Export.ToString(),
			Label = HandlerActions.Export.ToString(),
			Look = "primary",
			Color = "default"
		};

        var everythingExport = new SyncActionButton()
        {
            Key = HandlerActions.Export.ToString(),
            Label = HandlerActions.Export.ToString(),
            Look = "primary",
            Color = "default",
            Children = [
                    new () {
                        Key = HandlerActions.Export.ToString(),
                        Label = $"{HandlerActions.Export}Clean",
                        Clean = true
                    }
                ]
        };

		// TODO: we need to load in additional action groups as needed from plugins.
		List<SyncActionButton> defaultButtons = [defaultReport, defaultImport, defaultExport];
        List<SyncActionButton> everythingButtons = [defaultReport, defaultImport, everythingExport];

        List<SyncActionGroup> actionGroups = [];

        var options = new SyncHandlerOptions(_configService.Settings.DefaultSet)
        {
            Group = _configService.Settings.UIEnabledGroups
        };

        var groups = _handlerFactory.GetValidHandlerGroupsAndIcons(options);

        foreach(var group in groups)
        {
            actionGroups.Add(new SyncActionGroup
            {
                GroupName = $"{group.Key}",
                Icon = group.Value,
                Key = group.Key.ToLowerInvariant(),
                Buttons = defaultButtons
            });
        }

        if (string.IsNullOrWhiteSpace(_configService.Settings.UIEnabledGroups) ||
            _configService.Settings.UIEnabledGroups.InvariantContains("all"))
        {
            actionGroups.Add(new SyncActionGroup
            {
                GroupName = "Everything",
                Icon = "icon-paper-plane-alt",
                Key = "all",
                Buttons = everythingButtons
            });
        }

        return actionGroups;



  //      List<SyncActionGroup> actions = [
		//	new SyncActionGroup
		//	{
		//		GroupName = "Settings",
		//		Icon = "icon-settings-alt",
		//		Key = "settings",
		//		Buttons = defaultButtons
		//	},
		//	new SyncActionGroup
		//	{
		//		GroupName = "Content",
		//		Icon = "icon-documents",
		//		Key = "content",
		//		Buttons = defaultButtons
		//	},
		//	new SyncActionGroup
		//	{
		//		GroupName = "Everything",
		//		Icon = "icon-paper-plane-alt",
		//		Key = "all",
		//		Buttons = everythingButtons
		//	}
		//];

  //      return actions;

	}


	public PerformActionResponse PerformAction(PerformActionRequest actionRequest)
    {
        if (Enum.TryParse(actionRequest.Action, out HandlerActions action) is false)
            throw new ArgumentException($"Invalid action {actionRequest.Action}");

        var handlers = _syncActionService.GetActionHandlers(action, actionRequest.Options)
            .ToList();

        if (action == HandlerActions.Export && string.IsNullOrWhiteSpace(actionRequest.RequestId))
        { 
            // first step in an export.
            if (actionRequest.Options?.Clean is true)
            {
                // clean the export folder.  
                _syncActionService.CleanExportFolder();
			}
        }

        Guid requestId = GetRequestId(actionRequest);

		HubClientService? hubClient = default;
		if (actionRequest.Options?.ClientId != null)
		{
			hubClient = new HubClientService(_hubContext, actionRequest.Options.ClientId);
		}
		uSyncCallbacks callbacks = hubClient?.Callbacks() ?? new uSyncCallbacks(null, null);

		if (actionRequest.StepNumber >= handlers.Count)
        {
            var finalActions = _syncManagementCache.GetCachedActions(requestId);

            // when complete we clean out our action cache.
            _syncManagementCache.Clear(requestId);

            callbacks?.Update?.Invoke("Finished", 1, 1);

            // finished. 
            return new PerformActionResponse
            {
                RequestId = requestId.ToString(),
                Actions = finalActions.Select(x => x.ToActionView()),
                Complete = true,
                Status = GetSummaries(handlers, actionRequest.StepNumber, finalActions)
            };
        }

        var currentHandler = handlers[actionRequest.StepNumber];
        var method = GetHandlerMethod(action);


        var handlerOptions = new SyncActionOptions()
        {
            Folders = _configService.GetFolders(),
            Set = actionRequest.Options?.Set ?? _configService.Settings.DefaultSet,
            Force = actionRequest.Options?.Force ?? false,
            Actions = new List<uSyncAction>(),
            Handler = currentHandler.Alias
        };

        var results = method(handlerOptions, callbacks);
        _syncManagementCache.CacheItems(requestId, results.Actions, false);

        return new PerformActionResponse
        {
            RequestId = requestId.ToString(),
            Actions = results.Actions.Select(x => x.ToActionView()),
            Status = GetSummaries(handlers, actionRequest.StepNumber, results.Actions.ToList() ),
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

    private IEnumerable<SyncHandlerSummary> GetSummaries(List<SyncHandlerView> handlers, int step, List<uSyncAction> actions)
    {
        var nextStep = step + 1;
        for (int n = 0; n < handlers.Count; n++)
        {
            var handlerActions = actions.Where(x => x.HandlerAlias?.Equals(handlers[n].Alias, StringComparison.OrdinalIgnoreCase) is true).ToList();

            yield return new SyncHandlerSummary
            {
                Name = handlers[n].Name,
                Icon = handlers[n].Icon,
                Status = n < nextStep ? HandlerStatus.Complete :
                      n == nextStep ? HandlerStatus.Processing : HandlerStatus.Pending,
                Changes = handlerActions.Count(x => x.Change > Core.ChangeType.NoChange),
                InError = handlerActions.Any(x => x.Change >= Core.ChangeType.Fail)
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
