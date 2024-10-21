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

public partial class uSyncService
{
    private string[] GetFolderFromOptions(uSyncImportOptions options)
    {
        if (options.Folders?.Length > 0 is true)
            return options.Folders;

        // return the default. 
        return _uSyncConfig.GetFolders();
    }


    /// <summary>
    ///  Run a report for a given handler 
    /// </summary>
    [Obsolete("use ReportHandlerAsync will be removed in v16")]
    public IEnumerable<uSyncAction> ReportHandler(string handler, uSyncImportOptions options)
        => ReportHandlerAsync(handler, options).Result;
    
    public async Task<IEnumerable<uSyncAction>> ReportHandlerAsync(string handler, uSyncImportOptions options)
    {
        var handlerPair = _handlerFactory.GetValidHandler(handler, new SyncHandlerOptions
        {
            Set = options.HandlerSet,
            Action = HandlerActions.Report
        });

        if (handlerPair == null) return [];
        var folders = GetHandlerFolders(GetFolderFromOptions(options), handlerPair.Handler);

        return await handlerPair.Handler.ReportAsync(folders, handlerPair.Settings, options.Callbacks?.Update);
    }

    /// <summary>
    ///  run an import for a given handler 
    /// </summary>
    [Obsolete("use ImportHandlerAsync will be removed in v16")]
    public IEnumerable<uSyncAction> ImportHandler(string handlerAlias, uSyncImportOptions options)
        => ImportHandlerAsync(handlerAlias, options).Result;

    public async Task<IEnumerable<uSyncAction>> ImportHandlerAsync(string handlerAlias, uSyncImportOptions options)
    {
        try
        {
            _importSemaphoreLock.Wait();
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

                var results = await handlerPair.Handler.ImportAllAsync(folders, handlerPair.Settings, options);

                // _logger.LogDebug("< Import Handler {handler}", handlerAlias);

                scope?.Complete();

                return results;
            }
        }
        finally
        {
            _importSemaphoreLock.Release();
        }
    }

    /// <summary>
    ///  perform the post import actions for a handler 
    /// </summary>
    [Obsolete("use PerformPostImportAsync will be removed in v16")]
    public IEnumerable<uSyncAction> PerformPostImport(string[] folders, string handlerSet, IEnumerable<uSyncAction> actions)
        => PerformPostImportAsync(folders, handlerSet, actions).GetAwaiter().GetResult();

    public async Task<IEnumerable<uSyncAction>> PerformPostImportAsync(string[] folders, string handlerSet, IEnumerable<uSyncAction> actions)
    {
        try
        {
            _importSemaphoreLock.Wait();
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

    /// <summary>
    ///  run an export for a given handler 
    /// </summary>
    [Obsolete("use ExportHandlerAsync will be removed in v16")]
    public IEnumerable<uSyncAction> ExportHandler(string handler, uSyncImportOptions options)
        => ExportHandlerAsync(handler, options).Result;

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

    /// <summary>
    ///  Start a bulk run, fires events, and for exports writes the version file.
    /// </summary>
    public void StartBulkProcess(HandlerActions action)
    {
        switch (action)
        {
            case HandlerActions.Export:
                _mutexService.FireBulkStarting(new uSyncExportStartingNotification());
                break;
            case HandlerActions.Import:
                // cleans any caches we might have set.
                _appCache.ClearByKey("usync_");

                _mutexService.FireBulkStarting(new uSyncImportStartingNotification());
                break;
            case HandlerActions.Report:
                _mutexService.FireBulkStarting(new uSyncReportStartingNotification());
                break;
        }
    }

    /// <summary>
    ///  Complete a bulk run, fire the event so other things know we have done it.
    /// </summary>
    public void FinishBulkProcess(HandlerActions action, IEnumerable<uSyncAction> actions)
    {
        switch (action)
        {
            case HandlerActions.Export:
                WriteVersionFile(_uSyncConfig.GetWorkingFolder());
                _mutexService.FireBulkComplete(new uSyncExportCompletedNotification(actions));
                break;
            case HandlerActions.Import:
                _mutexService.FireBulkComplete(new uSyncImportCompletedNotification(actions));
                break;
            case HandlerActions.Report:
                _mutexService.FireBulkComplete(new uSyncReportCompletedNotification(actions));
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
