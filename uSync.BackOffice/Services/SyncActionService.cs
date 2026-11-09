using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
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

    private readonly ISyncConfigService _uSyncConfig;
    private readonly ISyncService _uSyncService;
    private readonly ISyncHandlerFactory _handlerFactory;
    private readonly ISyncFileService _syncFileService;

    private readonly string _uSyncTempPath;

    public SyncActionService(
        ILogger<SyncActionService> logger,
        ISyncConfigService uSyncConfig,
        ISyncService uSyncService,
        ISyncHandlerFactory handlerFactory,
        ISyncFileService syncFileService,
        Umbraco.Cms.Core.Hosting.IHostingEnvironment hostingEnvironment,
        IWebHostEnvironment webHostEnvironment)
    {
        _uSyncConfig = uSyncConfig;
        _uSyncService = uSyncService;
        _handlerFactory = handlerFactory;
        _syncFileService = syncFileService;
        _logger = logger;

        // temp folder, if this is running in the background, we might not 
        // have the hostingEnvironment available.
        _uSyncTempPath =
            Path.GetFullPath(
                Path.Combine(
                    hostingEnvironment?.LocalTempPath ?? 
                    webHostEnvironment.ContentRootPath
                , "uSync", "FileImport")
                );
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

        var (set, folders) = GetSetAndFolders(options);
        var importOptions = new uSyncImportOptions
        {
            Callbacks = callbacks,
            HandlerSet = set,
            Folders = folders
        };

        var actions = (await _uSyncService.ReportHandlerAsync(options.Handler, importOptions)).ToList();

        if (_uSyncConfig.Settings.SummaryDashboard || actions.Count > _uSyncConfig.Settings.SummaryLimit)
            actions = [.. actions.ConvertToSummary(_uSyncConfig.Settings.SummaryDashboard)];

        return new SyncActionResult(actions);
    }

    public async Task<SyncActionResult> ImportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks)
    {
        if (options.Handler is null) return new();

        var (set, folders) = GetSetAndFolders(options);
        var importOptions = new uSyncImportOptions
        {
            Callbacks = callbacks,
            HandlerSet = set,
            Folders = folders,
            PauseDuringImport = true,
            Flags = options.GetImportFlags()
        };

        var actions = (await _uSyncService.ImportHandlerAsync(options.Handler, importOptions)).ToList();

        if (_uSyncConfig.Settings.SummaryDashboard || actions.Count > _uSyncConfig.Settings.SummaryLimit)
            actions = [.. actions.ConvertToSummary(_uSyncConfig.Settings.SummaryDashboard)];

        return new SyncActionResult(actions);
    }

    public async Task<SyncActionResult> ImportPostAsync(SyncFinalActionRequest request)
    {
        var (set, folders) = GetSetAndFolders(request.ActionOptions);
        var actions = await _uSyncService.PerformPostImportAsync(folders, set, request.Actions);

        if (request.Callbacks is not null)
            await request.Callbacks.RaiseUpdateAsync("Post Import Complete", 1, 1);
        return new SyncActionResult([.. actions.Where(x => x.Change > Core.ChangeType.NoChange)]);
    }

    public async Task<SyncActionResult> ExportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks)
    {
        if (options.Handler is null) return new();

        var (set, folders) = GetSetAndFolders(options);
        var importOptions = new uSyncImportOptions
        {
            Callbacks = callbacks,
            HandlerSet = set,
            Folders = folders
        };

        var actions = (await _uSyncService.ExportHandlerAsync(options.Handler, importOptions)).ToList();

        if (_uSyncConfig.Settings.SummaryDashboard || actions.Count > _uSyncConfig.Settings.SummaryLimit)
            actions = [.. actions.ConvertToSummary(_uSyncConfig.Settings.SummaryDashboard)];

        return new SyncActionResult(actions);
    }

    /// <summary>
    ///  the handler set for these options, and the folders that set reads and writes.
    /// </summary>
    private (string Set, string[] Folders) GetSetAndFolders(SyncActionOptions options)
    {
        var set = options.GetSetOrDefault(_uSyncConfig.Settings.DefaultSet);
        return (set, options.GetFoldersOrDefault(_uSyncConfig.GetFolders(set)));
    }

    public void CleanExportFolder()
        => CleanExportFolder(null);

    private void CleanExportFolder(string? set)
    {
        try
        {
            var workingFolder = _uSyncConfig.GetWorkingFolder();
            var folder = _uSyncConfig.GetFolders(
                string.IsNullOrWhiteSpace(set) ? _uSyncConfig.Settings.DefaultSet : set).Last();

            if (IsSharedFolder(folder, workingFolder))
            {
                _logger.LogWarning("[uSync] Not cleaning {folder} for set {set}: the folder is, or contains, one of the global uSync folders. Give the set its own export folder (the last folder in its list).", folder, set);
                return;
            }

            _uSyncService.CleanExportFolder(folder);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[uSync] Failed to clean the export folder");
        }
    }

    /// <summary>
    ///  a set's export folder is shared if it is (or contains) one of the global folders,
    ///  other than the working folder, which is what a normal clean export empties.
    /// </summary>
    private bool IsSharedFolder(string folder, string workingFolder)
    {
        var target = NormalisePath(folder);
        if (target == NormalisePath(workingFolder)) return false;

        return _uSyncConfig.GetFolders()
            .Select(NormalisePath)
            .Any(x => x.StartsWith(target, StringComparison.OrdinalIgnoreCase));
    }

    private string NormalisePath(string folder)
        => _syncFileService.GetAbsPath(folder).TrimEnd('/', '\\') + Path.DirectorySeparatorChar;

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

    // keyed by requestId rather than a single shared timer, so concurrent runs
    // (and the multiple HTTP calls that make up one stepped run) each track
    // their own elapsed time without stomping on one another.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Stopwatch> _timers = new();

    /// <inheritdoc/>
    public async Task StartProcessAsync(SyncStartActionRequest request)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("[uSync] - Starting {action} process", request.HandlerAction);

        if (request.RequestId is Guid requestId)
            _timers[requestId] = Stopwatch.StartNew();

        await _uSyncService.StartBulkProcessAsync(request.HandlerAction);

        if (request.HandlerAction == HandlerActions.Export && request.Clean is true)
        {
            // clean the export folder (the set's own folder, if it has one).  
            CleanExportFolder(request.Set);
        }

    }

    /// <inheritdoc/>
    public async Task StartProcessAsync(HandlerActions action)
        => await StartProcessAsync(new SyncStartActionRequest { HandlerAction = action, Username = "", Clean = false });

    /// <inheritdoc/>
    public async Task<SyncActionResult> FinishProcessAsync(SyncFinalActionRequest request)
    {
        var (_, folders) = GetSetAndFolders(request.ActionOptions);
        await _uSyncService.FinishBulkProcessAsync(request.HandlerAction, request.ActionOptions.Group, folders.Last(), request.Actions);

        long elapsed = 0;
        if (_timers.TryRemove(request.RequestId, out var timer))
        {
            timer.Stop();
            elapsed = timer.ElapsedMilliseconds;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("[uSync] finished {action} process ({changes}/{count} changes) in ({time:#,#}ms)",
                request.HandlerAction, request.Actions.CountChanges(), request.Actions.Count(), elapsed);
        }

        if (request.Callbacks is not null)
            await request.Callbacks.RaiseUpdateAsync($"{request.HandlerAction} completed ({elapsed:#,#}ms)", 1, 1);

        // for speed we return an empty list. the merge will just take 
        // what we where passed in, and we avoid a whole copy and compare step
        return new SyncActionResult([]);
    }

    public Stream GetExportFolderAsStream()
    {
        return _uSyncService.CompressFolder(_uSyncConfig.GetWorkingFolder());
    }

    public async Task<UploadImportResult> UnpackImportFromStreamAsync(Stream stream)
    {
        var tempFolder = Path.Combine(_uSyncTempPath, Path.GetFileNameWithoutExtension(Path.GetRandomFileName())) 
            ?? $"{_uSyncTempPath}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) ?? Guid.NewGuid().ToString()}";

        Directory.CreateDirectory(tempFolder);

        try
        {
            _uSyncService.DeCompressFile(stream, tempFolder);

            var errors = await _syncFileService.VerifyFolderAsync(tempFolder,
                _uSyncConfig.Settings.DefaultExtension);

            if (errors.Count > 0)
            {
                return new UploadImportResult
                {
                    Success = false,
                    Errors = errors
                };
            }

            _uSyncService.ReplaceFiles(tempFolder, _uSyncConfig.GetWorkingFolder(), true);

            return new UploadImportResult
            {
                Success = true
            };
        }
        catch
        {
            throw;
        }
        finally
        {
            _syncFileService.DeleteFolder(tempFolder);
        }

        throw new Exception("Failed to import");
    }
}
