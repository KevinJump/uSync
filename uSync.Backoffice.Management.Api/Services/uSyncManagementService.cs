using Microsoft.AspNetCore.SignalR;

using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Backoffice.Management.Api.Extensions;
using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Hubs;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.BackOffice.Tracker;

namespace uSync.Backoffice.Management.Api.Services;

/// <summary>
///  the thing that does the imports, reports, and exports. 
/// </summary>
internal class uSyncManagementService : ISyncManagementService
{
    private readonly ISyncActionService _syncActionService;
    private readonly ISyncManagementCache _syncManagementCache;
    private readonly IHubContext<SyncHub> _hubContext;
    private readonly ISyncConfigService _configService;
    private readonly ISyncVersionFileService _syncVersionFileService;

    private readonly ISyncHandlerFactory _handlerFactory;

    private readonly ILongRunningOperationService _longRunningOperationService;

    private readonly ISyncTrackerService _syncTrackerService;

    public uSyncManagementService(
        ISyncActionService syncActionService,
        ISyncConfigService configService,
        ISyncVersionFileService syncVersionFileService,
        ISyncManagementCache syncManagementCache,
        IHubContext<SyncHub> hubContext,
        ISyncHandlerFactory handlerFactory,
        ILongRunningOperationService longRunningOperationService,
        ISyncTrackerService syncTrackerService)
    {
        _syncActionService = syncActionService;
        _configService = configService;
        _syncVersionFileService = syncVersionFileService;
        _syncManagementCache = syncManagementCache;
        _hubContext = hubContext;
        _handlerFactory = handlerFactory;
        _longRunningOperationService = longRunningOperationService;
        _syncTrackerService = syncTrackerService;
    }

    [Obsolete("Use GetActions(string setName) instead, this will be removed in v18")]
    public List<SyncActionGroup> GetActions()
        => GetActions(_configService.Settings.DefaultSet);

    [Obsolete("Use GetActionsAsync(string setName) instead, this will be removed in v19")]
    public List<SyncActionGroup> GetActions(string setName)
        => GetActionsAsync(setName).Result;

    /// <summary>
    ///  Gets the list of available actions
    /// </summary>
    public async Task<List<SyncActionGroup>> GetActionsAsync(string setName)
    {
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

        var everythingImport = new SyncActionButton()
        {
            Key = HandlerActions.Import.ToString(),
            Label = HandlerActions.Import.ToString(),
            Look = "primary",
            Color = "positive",
            Children = [
                    new () {
                        Key = HandlerActions.Import.ToString(),
                        Label = $"{HandlerActions.Import}Force",
                        Force = true
                    },
                    new () {
                        Key = HandlerActions.Import.ToString(),
                        Label = $"{HandlerActions.Import}File",
                        File = true
                    }
                ]
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
                    },
                    new () {
                        Key = HandlerActions.Export.ToString(),
                        Label = $"{HandlerActions.Export}File",
                        File = true,
                    }
                ]
        };

        List<SyncActionButton> defaultButtons = [defaultReport, defaultImport, defaultExport];
        List<SyncActionButton> everythingButtons = [defaultReport, everythingImport, everythingExport];

        List<SyncActionGroup> actionGroups = [];

        var options = new SyncHandlerOptions(setName)
        {
            Group = _configService.Settings.UIEnabledGroups
        };

        var groups = _handlerFactory.GetValidHandlerGroupsAndIcons(options);

        foreach (var group in groups)
        {
            var groupAlias = group.Key.ToLowerInvariant();

            actionGroups.Add(new SyncActionGroup
            {
                GroupName = $"{group.Key}",
                Icon = group.Value,
                Key = group.Key.ToLowerInvariant(),
                Buttons = defaultButtons,
                LastSync = await _syncTrackerService.GetLastSync(groupAlias)
            });
        }

        if (string.IsNullOrWhiteSpace(_configService.Settings.UIEnabledGroups) ||
            _configService.Settings.UIEnabledGroups.InvariantContains(BackOffice.uSync.EverythingGroupName))
        {
            actionGroups.Add(new SyncActionGroup
            {
                GroupName = "Everything",
                Icon = "icon-paper-plane-alt",
                Key = BackOffice.uSync.EverythingGroupName,
                Buttons = everythingButtons,
                LastSync = await _syncTrackerService.GetLastSync(BackOffice.uSync.EverythingGroupName)

            });
        }

        return actionGroups;
    }


    public async Task<PerformActionResponse> PerformActionAsync(PerformActionRequest actionRequest, IUser? user)
    {
        if (_configService.Settings.ProcessingMode == SyncProcessingMode.Background)
        {
            return await PerformBackgroundActionAsync(actionRequest, user);
        }

        return await PerformActionInternalAsync(string.IsNullOrWhiteSpace(actionRequest.RequestId), actionRequest, user);
    }


    private async Task<PerformActionResponse> PerformBackgroundActionAsync(PerformActionRequest actionRequest, IUser? user)
    {
        if (Enum.TryParse(actionRequest.Action, out HandlerActions action) is false)
            throw new ArgumentException($"Invalid action {actionRequest.Action}");

        var handlers = _syncActionService.GetActionHandlers(action, actionRequest.Options)
            .ToList();

        actionRequest.RequestId = GetRequestId(actionRequest).ToString();

        var enqueAttempt = await _longRunningOperationService.RunAsync(
            actionRequest.RequestId,
            async _ => await ProcessAllActions(actionRequest, user),
            allowConcurrentExecution: false);

        return new PerformActionResponse
        {
            RequestId = Guid.NewGuid().ToString(),
            Complete = true,
            InBackground = true
        };
    }

    public async Task ProcessAllActions(PerformActionRequest request, IUser? user)
    {
        var result = default(PerformActionResponse);

        // this is a break should something get stuck in a loop.
        // in theory there is only ~ 13/14 handlers so 100 means 
        // its gone wrong by a bit. 
        var count = 0;

        do
        {
            result = await PerformActionInternalAsync(count == 0, request, user);
            request.RequestId = result.RequestId;
            request.StepNumber++;
            count++;
        } while (result.Complete is false && count < 100);

        
    }

    private async Task<PerformActionResponse> PerformActionInternalAsync(bool isFirstRequest, PerformActionRequest actionRequest, IUser? user)
    {
        if (Enum.TryParse(actionRequest.Action, out HandlerActions action) is false)
            throw new ArgumentException($"Invalid action {actionRequest.Action}");

        var handlers = _syncActionService.GetActionHandlers(action, actionRequest.Options)
            .ToList();

        if (isFirstRequest)
        {
            await _syncActionService.StartProcessAsync(new SyncStartActionRequest
            {
                Username = user?.Username,
                HandlerAction = action,
                Clean = actionRequest.Options?.Clean ?? false
            });
        }

        Guid requestId = GetRequestId(actionRequest);
        uSyncCallbacks callbacks = GetCallbacksFromRequest(actionRequest);

        var handlerOptions = new SyncActionOptions()
        {
            Folders = _configService.GetFolders(),
            Set = actionRequest.Options?.Set ?? _configService.Settings.DefaultSet,
            Group = actionRequest.Options?.Group ?? "all",
            Force = actionRequest.Options?.Force ?? false,
            Actions = [],
        };

        if (actionRequest.StepNumber >= handlers.Count)
        {
            var actions = await PerformFinalSteps(requestId, action, handlerOptions, callbacks, user?.Username);
            return SummerizeCompleteProcess(actionRequest, action, handlers, requestId, callbacks, actions);
        }


        handlerOptions.Handler = handlers[actionRequest.StepNumber].Alias;
        var method = GetHandlerMethodAsync(action);

        var results = await method(handlerOptions, callbacks);
        
        _syncManagementCache.CacheItems(requestId, results.Actions, false);
        var allActions = _syncManagementCache.GetCachedActions(requestId);

        var summaries = GetSummaries(action, handlers, actionRequest.StepNumber, allActions);
        callbacks.Callback?.Invoke(new SyncProgressSummary(summaries, "Processing " + action.ToString(), handlers.Count));

        return new PerformActionResponse
        {
            RequestId = requestId.ToString(),
            Actions = allActions.Where(x => x.Change != Core.ChangeType.Hidden).Select(x => x.AsActionView()),
            Status = summaries,
            Complete = false
        };
    }

    private static PerformActionResponse SummerizeCompleteProcess(PerformActionRequest actionRequest, HandlerActions action, List<SyncHandlerView> handlers, Guid requestId, uSyncCallbacks callbacks, List<uSyncAction> actions)
    {
        var finalSummary = GetSummaries(action, handlers, actionRequest.StepNumber + 1, actions);
        var actionViews = actions.Select(x => x.AsActionView());

        callbacks?.Callback?.Invoke(new SyncProgressSummary(finalSummary, "Completed", handlers.Count));
        callbacks?.Complete?.Invoke(requestId, "Sync complete", true, actionViews);

        // finished. 
        return new PerformActionResponse
        {
            RequestId = requestId.ToString(),
            Actions = actionViews,
            Complete = true,
            Status = finalSummary
        };
    }

    private uSyncCallbacks GetCallbacksFromRequest(PerformActionRequest request)
    {
        var hubClient = request.Options?.ClientId == null 
            ? null 
            : new HubClientService(_hubContext, request.Options.ClientId);

        return hubClient?.Callbacks() ?? new uSyncCallbacks(null, null);
    }

    private async Task<List<uSyncAction>> PerformFinalSteps(Guid requestId, HandlerActions action, SyncActionOptions options, uSyncCallbacks callbacks, string? username)
    {
        var finalActions = _syncManagementCache.GetCachedActions(requestId);
        var finalSteps = GetFinalStep(action);

        var request = new SyncFinalActionRequest
        {
            RequestId = requestId,
            HandlerAction = action,
            ActionOptions = options,
            Actions = finalActions,
            Callbacks = callbacks,
            Username = username ?? ""
        };

        List<uSyncAction> actionResults = [.. finalActions];

        foreach (var step in finalSteps)
        {
            request.Actions = actionResults;
            var result = await step(request);
            // merge the actions here...
            actionResults = actionResults.Merge(result.Actions);
        }

        // when complete we clean out our action cache.
        _syncManagementCache.Clear(requestId);

        return [.. actionResults.Where(x => x.Change != Core.ChangeType.Hidden)];
    }

    private List<Func<SyncFinalActionRequest, Task<SyncActionResult>>> GetFinalStep(HandlerActions action)
    {
        var steps = new List<Func<SyncFinalActionRequest, Task<SyncActionResult>>>();

        if (action == HandlerActions.Import)
        {
            steps.Add(_syncActionService.ImportPostAsync);
        }

        steps.Add(_syncActionService.FinishProcessAsync);
        return steps;
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

    private static IEnumerable<SyncHandlerSummary> GetSummaries(HandlerActions action, List<SyncHandlerView> handlers, int step, List<uSyncAction> actions)
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
                Changes = handlerActions.CountChanges(),
                InError = handlerActions.ContainsErrors()
            };
        }

        if (action == HandlerActions.Import)
        {
            yield return new SyncHandlerSummary
            {
                Name = $"Post Import",
                Icon = "icon-directions",
                Status = nextStep == handlers.Count ? HandlerStatus.Processing :
                        nextStep > handlers.Count ? HandlerStatus.Complete : HandlerStatus.Pending,
                Changes = 0,
                InError = false
            };
        }
    }


    public Func<SyncActionOptions, uSyncCallbacks, Task<SyncActionResult>> GetHandlerMethodAsync(HandlerActions action)
    {
        return action switch
        {
            HandlerActions.Import => _syncActionService.ImportHandlerAsync,
            HandlerActions.Report => _syncActionService.ReportHandlerAsync,
            HandlerActions.Export => _syncActionService.ExportHandlerAsync,
            _ => throw new InvalidOperationException($"Unknown method {action}"),
        };
    }

    /// <summary>
    ///  compress the uSync folder into a zip file , return the stream
    /// </summary>
    /// <returns></returns>
    public Stream CompressExportFolder()
        => _syncActionService.GetExportFolderAsStream();

    /// <summary>
    ///  take a zip file as a stream expand it over the current uSync folder. 
    /// </summary>
    public UploadImportResult UnpackStream(Stream stream)
        => _syncActionService.UnpackImportFromStreamAsync(stream).Result;

    public async Task<SyncFileVersionCheckResult> GetSyncFileInfo()
        => await _syncVersionFileService.GetSyncFileInfo(_configService.GetWorkingFolder());

}
