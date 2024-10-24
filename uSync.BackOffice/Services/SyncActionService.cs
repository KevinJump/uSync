using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Extensions;
using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice.Services;

/// <summary>
///  handling most of the action logic. 
/// </summary>
/// <remarks>
///  making the ApiControllers dumber, so we can migrate easier.
/// </remarks>
internal class SyncActionService : ISyncActionService
{
    private readonly ILogger<SyncActionService> _logger;

    private readonly uSyncConfigService _uSyncConfig;
    private readonly uSyncService _uSyncService;
    private readonly SyncHandlerFactory _handlerFactory;
    private readonly ISyncFileService _syncFileService;

    public SyncActionService(
        ILogger<SyncActionService> logger,
        uSyncConfigService uSyncConfig,
        uSyncService uSyncService,
        SyncHandlerFactory handlerFactory,
        ISyncFileService syncFileService)
    {
        _uSyncConfig = uSyncConfig;
        _uSyncService = uSyncService;
        _handlerFactory = handlerFactory;
        _syncFileService = syncFileService;
        _logger = logger;
    }

    public IEnumerable<SyncHandlerView> GetActionHandlers(HandlerActions action, uSyncOptions? options)
    {
        var handlerSet = options.GetSetOrDefault(_uSyncConfig.Settings.DefaultSet);

        var handlerOptions = new SyncHandlerOptions
        {
            Group = options.GetGroupOrDefault(_uSyncConfig.Settings.UIEnabledGroups),
            Action = action,
            Set = handlerSet
        };

        return _handlerFactory
            .GetValidHandlers(handlerOptions)
            .Select(x => x.ToSyncHandlerView(handlerSet));
    }

    public async Task<SyncActionResult> ReportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks)
    {
        if (options.Handler is null) return new();

        var importOptions = new uSyncImportOptions
        {
            Callbacks = callbacks,
            HandlerSet = options.GetSetOrDefault(_uSyncConfig.Settings.DefaultSet),
            Folders = options.GetFoldersOrDefault(_uSyncConfig.GetFolders()).Select(MakeValidImportFolder).ToArray()
        };

		var actions = (await _uSyncService.ReportHandlerAsync(options.Handler, importOptions)).ToList();

        if (_uSyncConfig.Settings.SummaryDashboard || actions.Count > _uSyncConfig.Settings.SummaryLimit)
            actions = actions.ConvertToSummary(_uSyncConfig.Settings.SummaryDashboard).ToList();

        return new SyncActionResult(actions);
    }

    public async Task<SyncActionResult> ImportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks)
    {
        if (options.Handler is null) return new();

        var importOptions = new uSyncImportOptions
        {
            Callbacks = callbacks,
            HandlerSet = options.GetSetOrDefault(_uSyncConfig.Settings.DefaultSet),
            Folders =  options.GetFoldersOrDefault(_uSyncConfig.GetFolders()),
            PauseDuringImport = true,
            Flags = options.GetImportFlags()
        };

        var actions = (await _uSyncService.ImportHandlerAsync(options.Handler, importOptions)).ToList();

        if (_uSyncConfig.Settings.SummaryDashboard || actions.Count > _uSyncConfig.Settings.SummaryLimit)
            actions = actions.ConvertToSummary(_uSyncConfig.Settings.SummaryDashboard).ToList();

        return new SyncActionResult(actions);
    }

    public async Task<SyncActionResult> ImportPostAsync(SyncActionOptions options, uSyncCallbacks? callbacks)
    {
		var actions = await _uSyncService.PerformPostImportAsync(
            options.GetFoldersOrDefault(_uSyncConfig.GetFolders()),
            options.GetSetOrDefault(_uSyncConfig.Settings.DefaultSet),
            options.Actions);

        callbacks?.Update?.Invoke("Import Complete", 1, 1);

        return new SyncActionResult(actions.Where(x => x.Change > Core.ChangeType.NoChange).ToList());
    }

    public async Task<SyncActionResult> ExportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks)
    {
        if (options.Handler is null) return new();

        var importOptions = new uSyncImportOptions
        {
            Callbacks = callbacks,
            HandlerSet = options.GetSetOrDefault(_uSyncConfig.Settings.DefaultSet),
            Folders = options.GetFoldersOrDefault(_uSyncConfig.GetFolders())
        };

        var actions = (await _uSyncService.ExportHandlerAsync(options.Handler, importOptions)).ToList();

        if (_uSyncConfig.Settings.SummaryDashboard || actions.Count > _uSyncConfig.Settings.SummaryLimit)
            actions = actions.ConvertToSummary(_uSyncConfig.Settings.SummaryDashboard).ToList();

        return new SyncActionResult(actions);
    }

    public void CleanExportFolder()
    {
        try
        {
            _uSyncService.CleanExportFolder(_uSyncConfig.GetWorkingFolder());
        }
        catch
        {
            // 
        }
    }

    private string MakeValidImportFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return _uSyncConfig.GetWorkingFolder();

        // else check its a valid folder. 
        var fullPath = _syncFileService.GetAbsPath(folder);
        var fullRoot = _syncFileService.GetAbsPath(_uSyncConfig.GetWorkingFolder());

        var rootParent = Path.GetDirectoryName(fullRoot.TrimEnd(['/', '\\']));
        
        // _logger.LogDebug("Import Folder: {fullPath} {rootPath} {fullRoot}", fullPath, rootParent, fullRoot);

        if (rootParent is not null && fullPath.StartsWith(rootParent))
        {
            // _logger.LogDebug("Using Custom Folder: {fullPath}", folder);
            return folder;
        }


        return string.Empty;
    }

    /// <inheritdoc/>
    public async Task StartProcessAsync(HandlerActions action)
        => await _uSyncService.StartBulkProcessAsync(action);

    /// <inheritdoc/>
    public async Task FinishProcessAsync(HandlerActions action, IEnumerable<uSyncAction> actions, string username)
    {
        await _uSyncService.FinishBulkProcessAsync(action, actions);

        _logger.LogInformation("{user} finished {action} process ({changes} changes)",
            username, action, actions.Count());
    }

}
