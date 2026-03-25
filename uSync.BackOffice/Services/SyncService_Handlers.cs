using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using uSync.BackOffice.Extensions;
using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice;

/// <summary>
///  actions on individual handlers. 
/// </summary>

public partial class SyncService
{
    private string[] GetFolderFromOptions(uSyncImportOptions options)
    {
        if (options.Folders?.Length > 0 is true)
            return options.Folders;

        // return the default. 
        return _uSyncConfig.GetFolders();
    }

    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> ReportHandlerAsync(string handler, uSyncImportOptions options)
    {
        var handlerPair = _handlerFactory.GetValidHandler(handler, new SyncHandlerOptions
        {
            Set = options.HandlerSet,
            Action = HandlerActions.Report
        });

        if (handlerPair == null) return [];
        var folders = GetHandlerFolders(GetFolderFromOptions(options), handlerPair.Handler);

        var productionFile = $"{folders.Last()}.{_uSyncConfig.Settings.DefaultExtension}";
        if (_syncFileService.FileExists(productionFile))
            return await ReportMergedFile(productionFile, handlerPair, options);

        return await handlerPair.Handler.ReportAsync(folders, handlerPair.Settings, options.Callbacks?.Update);
    }

    private async Task<IEnumerable<uSyncAction>> ReportMergedFile(string filename, HandlerConfigPair handlerPair, uSyncImportOptions options)
    {
        var node = await _syncFileService.LoadXElementAsync(filename);
        return await handlerPair.Handler.ReportElementAsync(node, filename, handlerPair.Settings, options);
    }

    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> ImportHandlerAsync(string handlerAlias, uSyncImportOptions options)
    {
        if (await _importSemaphoreLock.WaitAsync(_importLockWaitTimeSpan) is false) 
            throw new TimeoutException($"Could not acquire import lock within the configured timespan of {_importLockWaitTimeSpan.TotalSeconds} seconds.");

        try
        {
            using (var pause = _mutexService.ImportPause(options.PauseDuringImport))
            {
                var handlerPair = _handlerFactory.GetValidHandler(handlerAlias, new SyncHandlerOptions
                {
                    Set = options.HandlerSet,
                    Action = HandlerActions.Import
                });

                if (handlerPair == null) return [];
                var folders = GetHandlerFolders(GetFolderFromOptions(options), handlerPair.Handler);

                // _logger.LogDebug("> Import Handler {handler}", handlerAlias);

                using var scope = _scopeProvider.CreateNotificationScope(
                    eventAggregator: _eventAggregator,
                    loggerFactory: _loggerFactory,
                    syncConfigService: _uSyncConfig,
                    syncEventService: _mutexService,
                    backgroundTaskQueue: _backgroundTaskQueue,
                    options.Callbacks?.Update);

                List<uSyncAction> results;

                var productionFile = $"{folders.Last()}.{_uSyncConfig.Settings.DefaultExtension}";
                if (_syncFileService.FileExists(productionFile))
                    results = [.. await ImportMergedFile(productionFile, handlerPair, options)];
                else
                    results = [.. await handlerPair.Handler.ImportAllAsync(folders, handlerPair.Settings, options)];

                scope?.Complete();
                return results;
            }
        }
        finally
        {
            _importSemaphoreLock.Release();
        }
    }

    private async Task<IEnumerable<uSyncAction>> ImportMergedFile(string filename, HandlerConfigPair handlerPair, uSyncImportOptions options)
    {
        var node = await _syncFileService.LoadXElementAsync(filename);
        return await handlerPair.Handler.ImportElementAsync(node, filename, handlerPair.Settings, options);
    }

    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> PerformPostImportAsync(string[] folders, string handlerSet, IEnumerable<uSyncAction> actions)
    {
        if (await _importSemaphoreLock.WaitAsync(_importLockWaitTimeSpan) is false)
            throw new TimeoutException($"Could not acquire import lock within the configured timespan of {_importLockWaitTimeSpan.TotalSeconds} seconds.");

        try
        {
            using (var pause = _mutexService.ImportPause(true))
            {
                var handlers = _handlerFactory.GetValidHandlers(new SyncHandlerOptions { Set = handlerSet, Action = HandlerActions.Import });
                return await PerformPostImportAsync(handlers, actions);
            }
        }
        finally
        {
            _importSemaphoreLock.Release();
        }
    }

    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> ExportHandlerAsync(string handler, uSyncImportOptions options)
    {
        var handlerPair = _handlerFactory.GetValidHandler(handler, new SyncHandlerOptions
        {
            Set = options.HandlerSet,
            Action = HandlerActions.Export
        });

        if (handlerPair == null) return [];
        var folders = GetHandlerFolders(GetFolderFromOptions(options), handlerPair.Handler);
        return await handlerPair.Handler.ExportAllAsync(folders, handlerPair.Settings, options.Callbacks?.Update);
    }

    /// <inheritdoc/>>
    public async Task StartBulkProcessAsync(HandlerActions action)
    {
        switch (action)
        {
            case HandlerActions.Export:
                await _mutexService.FireBulkStartingAsync(new uSyncExportStartingNotification());
                break;
            case HandlerActions.Import:
                // cleans any caches we might have set.
                _appCache.ClearByKey("usync_");
                await _mutexService.FireBulkStartingAsync(new uSyncImportStartingNotification());
                break;
            case HandlerActions.Report:
                await _mutexService.FireBulkStartingAsync(new uSyncReportStartingNotification());
                break;
        }
    }

    /// <inheritdoc/>>
    [Obsolete("Use the overload with the group parameter instead")]
    public async Task FinishBulkProcessAsync(HandlerActions action, IEnumerable<uSyncAction> actions)
        => await FinishBulkProcessAsync(action, uSync.EverythingGroupName, actions);

    /// <inheritdoc/>>
    public async Task FinishBulkProcessAsync(HandlerActions action, string? group, IEnumerable<uSyncAction> actions)
    {
        switch (action)
        {
            case HandlerActions.Export:
                await WriteVersionFileAsync(_uSyncConfig.GetWorkingFolder());
                await _mutexService.FireBulkCompleteAsync(new uSyncExportCompletedNotification(actions, group));
                break;
            case HandlerActions.Import:
                await _mutexService.FireBulkCompleteAsync(new uSyncImportCompletedNotification(actions, group));
                break;
            case HandlerActions.Report:
                await _mutexService.FireBulkCompleteAsync(new uSyncReportCompletedNotification(actions, group));
                break;
        }
    }

    /// <summary>
    ///  gets the physical folder for a handler. ( root + handler folder)
    /// </summary>
    private static string GetHandlerFolder(string rootFolder, ISyncHandler handler)
        => Path.Combine(rootFolder, handler.DefaultFolder);

    private static string[] GetHandlerFolders(string[] folders, ISyncHandler handler)
        => folders.Select(x => GetHandlerFolder(x, handler)).ToArray();

}
