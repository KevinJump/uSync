using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Logging;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers;

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
    private readonly SyncFileService _syncFileService;

    public SyncActionService(
        ILogger<SyncActionService> logger,
        uSyncConfigService uSyncConfig,
        uSyncService uSyncService,
        SyncHandlerFactory handlerFactory,
        SyncFileService syncFileService)
    {
        _uSyncConfig = uSyncConfig;
        _uSyncService = uSyncService;
        _handlerFactory = handlerFactory;
        _syncFileService = syncFileService;
        _logger = logger;
    }

    public IEnumerable<SyncHandlerView> GetActionHandlers(HandlerActions action, uSyncOptions? options)
    {
        var handlerGroup = string.IsNullOrWhiteSpace(options?.Group)
                       ? _uSyncConfig.Settings.UIEnabledGroups
                       : options.Group;

        var handlerSet = string.IsNullOrWhiteSpace(options?.Set)
            ? _uSyncConfig.Settings.DefaultSet
            : options.Set;

        return _handlerFactory.GetValidHandlers(new SyncHandlerOptions
        {
            Group = handlerGroup,
            Action = action,
            Set = handlerSet
        }).Select(x => new SyncHandlerView
        {
            Enabled = x.Handler.Enabled,
            Alias = x.Handler.Alias,
            Name = x.Handler.Name,
            Icon = x.Handler.Icon,
            Group = x.Handler.Group,
            Set = handlerSet
        });
    }

    public SyncActionResult ReportHandler(SyncActionOptions options, uSyncCallbacks? callbacks)
    {
        if (options.Handler is null) return new();

        var handlerSet = !string.IsNullOrWhiteSpace(options.Set)
                       ? options.Set : _uSyncConfig.Settings.DefaultSet;

        var folders = GetFolders(options);

		var actions = _uSyncService.ReportHandler(options.Handler,
            new uSyncImportOptions
            {
                Callbacks = callbacks,
                HandlerSet = handlerSet,
                Folders = folders.Select(MakeValidImportFolder).ToArray()
            }).ToList();

        if (_uSyncConfig.Settings.SummaryDashboard || actions.Count > _uSyncConfig.Settings.SummaryLimit)
            actions = actions.ConvertToSummary(_uSyncConfig.Settings.SummaryDashboard).ToList();

        return new SyncActionResult(actions);
    }

	private string[] GetFolders(SyncActionOptions options)
	{
		if (options.Folders.Length != 0)
			return options.Folders;

		return _uSyncConfig.GetFolders();
	}

	public SyncActionResult ImportHandler(SyncActionOptions options, uSyncCallbacks? callbacks)
    {
        if (options.Handler is null) return new();

        var handlerSet = !string.IsNullOrWhiteSpace(options.Set)
                  ? options.Set : _uSyncConfig.Settings.DefaultSet;

        var folders = GetFolders(options);

		var actions = _uSyncService.ImportHandler(options.Handler, new uSyncImportOptions
        {
            Callbacks = callbacks,
            HandlerSet = handlerSet,
            Folders = folders,
            PauseDuringImport = true,
            Flags = options.Force ? Core.Serialization.SerializerFlags.Force : Core.Serialization.SerializerFlags.None
        }).ToList();

        if (_uSyncConfig.Settings.SummaryDashboard || actions.Count > _uSyncConfig.Settings.SummaryLimit)
            actions = actions.ConvertToSummary(_uSyncConfig.Settings.SummaryDashboard).ToList();

        return new SyncActionResult(actions);
    }

    public SyncActionResult ImportPost(SyncActionOptions options, uSyncCallbacks? callbacks)
    {

        var handlerSet = !string.IsNullOrWhiteSpace(options.Set)
            ? options.Set : _uSyncConfig.Settings.DefaultSet;

        var folders = GetFolders(options);

		var actions = _uSyncService.PerformPostImport(
            folders,
            handlerSet,
            options.Actions);

        callbacks?.Update?.Invoke("Import Complete", 1, 1);

        return new SyncActionResult(actions.Where(x => x.Change > Core.ChangeType.NoChange).ToList());
    }

    public SyncActionResult ExportHandler(SyncActionOptions options, uSyncCallbacks? callbacks)
    {
        if (options.Handler is null) return new();

        var handlerSet = !string.IsNullOrWhiteSpace(options.Set)
            ? options.Set : _uSyncConfig.Settings.DefaultSet;

        var folders = GetFolders(options);

		var actions = _uSyncService.ExportHandler(options.Handler, new uSyncImportOptions
        {
            Callbacks = callbacks,
            HandlerSet = handlerSet,
            Folders = folders
        }).ToList();

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
    public void StartProcess(HandlerActions action)
        => _uSyncService.StartBulkProcess(action);

    /// <inheritdoc/>
    public void FinishProcess(HandlerActions action, IEnumerable<uSyncAction> actions, string username)
    {
        _uSyncService.FinishBulkProcess(action, actions);

        _logger.LogInformation("{user} finished {action} process ({changes} changes)",
            username, action, actions.Count());
    }

}
