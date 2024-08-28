using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

using uSync.BackOffice.Extensions;
using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice
{
	/// <summary>
	///  actions on individual handlers. 
	/// </summary>

	public partial class uSyncService
    {
        private string[] GetFolderFromOptions(uSyncImportOptions options)
        {
            if (options.Folders?.Length > 0 is true)
                return options.Folders;

            if (string.IsNullOrWhiteSpace(options.RootFolder) is false)
                return [options.RootFolder];

            // return the default. 
            return _uSyncConfig.GetFolders();
        }

        /// <summary>
        ///  Run a report for a given handler 
        /// </summary>
        public IEnumerable<uSyncAction> ReportHandler(string handler, uSyncImportOptions options)
        {
            var handlerPair = _handlerFactory.GetValidHandler(handler, new SyncHandlerOptions
            {
                Set = options.HandlerSet,
                Action = HandlerActions.Report
            });

            if (handlerPair == null) return [];
            var folders = GetHandlerFolders(GetFolderFromOptions(options), handlerPair.Handler);

            return handlerPair.Handler.Report(folders, handlerPair.Settings, options.Callbacks?.Update);
        }

        /// <summary>
        ///  run an import for a given handler 
        /// </summary>
        public IEnumerable<uSyncAction> ImportHandler(string handlerAlias, uSyncImportOptions options)
        {
            lock (_importLock)
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
                   
                    using var scope = _scopeProvider.CreateNotificationScope(
                        eventAggregator: _eventAggregator,
                        loggerFactory: _loggerFactory,
                        syncConfigService: _uSyncConfig,
                        syncEventService: _mutexService,
                        backgroundTaskQueue: _backgroundTaskQueue,
                        options.Callbacks?.Update);

                    using var supression = scope.SuppressScopeByConfig(_uSyncConfig);

                    var results = handlerPair.Handler.ImportAll(folders, handlerPair.Settings, options);

                    scope.Complete();

                    return results;
                }
            }
        }

        /// <summary>
        ///  perform the post import actions for a handler 
        /// </summary>
        [Obsolete("Pass array of folders, will be removed in v15")]
        public IEnumerable<uSyncAction> PerformPostImport(string rootFolder, string handlerSet, IEnumerable<uSyncAction> actions)
            => PerformPostImport([rootFolder], handlerSet, actions);

        /// <summary>
        ///  perform the post import actions for a handler 
        /// </summary>
        public IEnumerable<uSyncAction> PerformPostImport(string[] folders, string handlerSet, IEnumerable<uSyncAction> actions)
        {
            lock (_importLock)
            {
                using (var pause = _mutexService.ImportPause(true))
                {
                    var handlers = _handlerFactory.GetValidHandlers(new SyncHandlerOptions { Set = handlerSet, Action = HandlerActions.Import });
                    return PerformPostImport(handlers, actions);
                }
            }
        }

        /// <summary>
        ///  run an export for a given handler 
        /// </summary>
        public IEnumerable<uSyncAction> ExportHandler(string handler, uSyncImportOptions options)
        {
            var handlerPair = _handlerFactory.GetValidHandler(handler, new SyncHandlerOptions
            {
                Set = options.HandlerSet,
                Action = HandlerActions.Export
            });

            if (handlerPair == null) return [];

            var folders = GetHandlerFolders(GetFolderFromOptions(options), handlerPair.Handler);
            return handlerPair.Handler.ExportAll(folders, handlerPair.Settings, options.Callbacks?.Update);
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
                    WriteVersionFile(_uSyncConfig.GetRootFolder());
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
}
