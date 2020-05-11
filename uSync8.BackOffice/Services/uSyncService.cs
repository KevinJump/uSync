using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice
{

    /// <summary>
    ///  the service that does all the processing,
    ///  this forms the entry point as an API to 
    ///  uSync, it is where imports, exports and reports
    ///  are actually ran from. 
    /// </summary>
    public partial class uSyncService
    {
        public delegate void SyncEventCallback(SyncProgressSummary summary);

        private uSyncSettings settings;
        private readonly SyncHandlerFactory handlerFactory;
        private readonly IProfilingLogger logger;
        private SyncFileService syncFileService;

        public uSyncService(
            SyncHandlerFactory handlerFactory,
            IProfilingLogger logger,
            SyncFileService syncFileService)
        {
            this.handlerFactory = handlerFactory;

            this.syncFileService = syncFileService;

            this.settings = Current.Configs.uSync();
            this.logger = logger;

            uSyncConfig.Reloaded += BackOfficeConfig_Reloaded;

            uSyncTriggers.DoExport += USyncTriggers_DoExport;
            uSyncTriggers.DoImport += USyncTriggers_DoImport;
        }

        private void BackOfficeConfig_Reloaded(uSyncSettings settings)
        {
            this.settings = Current.Configs.uSync();
        }

        #region Reporting 

        /// <summary>
        ///  Report the changes for a folder
        /// </summary>
        /// <param name="folder">Folder to run the report for</param>
        /// <param name="handlerOptions">Options to use for the report - used to load the handlers.</param>
        /// <param name="callbacks">Callback functions to keep UI uptodate</param>
        /// <returns>List of actions detailing what would and wouldn't change</returns>
        public IEnumerable<uSyncAction> Report(string folder, SyncHandlerOptions handlerOptions, uSyncCallbacks callbacks = null)
        {
            if (handlerOptions == null) handlerOptions = new SyncHandlerOptions();
            handlerOptions.Action = HandlerActions.Report;

            var handlers = handlerFactory.GetValidHandlers(handlerOptions);
            return Report(folder, handlers, callbacks);
        }

        /// <summary>
        ///  Report the changes for a folder
        /// </summary>
        /// <param name="folder">Folder to run the report for</param>
        /// <param name="handlerAliases">List of Aliases for the sync handlers to use</param>
        /// <param name="callbacks">Callback functions to keep UI uptodate</param>
        /// <returns>List of actions detailing what would and wouldn't change</returns>
        public IEnumerable<uSyncAction> Report(string folder, IEnumerable<string> handlerAliases, uSyncCallbacks callbacks)
        {
            var handlers = handlerFactory.GetDefaultHandlers(handlerAliases);
            return Report(folder, handlers, callbacks);
        }

        /// <summary>
        ///  Report the changes for a folder
        /// </summary>
        /// <param name="folder">Folder to run the report for</param>
        /// <param name="handlers">List of SyncHandlers to use for the report</param>
        /// <param name="callbacks">Callback functions to keep UI uptodate</param>
        /// <returns>List of actions detailing what would and wouldn't change</returns>
        public IEnumerable<uSyncAction> Report(string folder, IEnumerable<ExtendedHandlerConfigPair> handlers, uSyncCallbacks callbacks)
        {

            var sw = Stopwatch.StartNew();

            fireBulkStarting(ReportStarting);

            logger.Debug<uSyncService>("Reporting For [{0}]", string.Join(",", handlers.Select(x => x.Handler.Name)));

            var actions = new List<uSyncAction>();

            var summary = new SyncProgressSummary(handlers.Select(x => x.Handler), "Reporting", handlers.Count());

            if (GlobalSettings.DebugMode && settings.ReportDebug)
            {
                logger.Warn<uSyncService>("Running Report Debug - this can be a slow process, don't enable unless you need it");
                // debug - full export into a dated folder. 
                summary.Message = "Debug: Creating Extract in Tracker folder";
                callbacks?.Callback?.Invoke(summary);
                this.Export($"~/uSync/Tracker/{DateTime.Now.ToString("yyyyMMdd_HHmmss")}/", handlers, callbacks);
            }

            foreach (var configuredHandler in handlers)
            {
                var handler = configuredHandler.Handler;
                var handlerSettings = configuredHandler.Settings;

                summary.Count++;

                summary.UpdateHandler(handler.Name, HandlerStatus.Processing, $"Reporting {handler.Name}", 0);

                callbacks?.Callback?.Invoke(summary);

                var handlerActions = handler.Report($"{folder}/{handler.DefaultFolder}", handlerSettings, callbacks?.Update);
                actions.AddRange(handlerActions);

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete, ChangeCount(handlerActions), ContainsErrors(handlerActions));
            }

            summary.Message = "Report Complete";
            callbacks?.Callback?.Invoke(summary);

            fireBulkComplete(ReportComplete, actions);
            sw.Stop();

            logger.Info<uSyncService>("uSync Report: {handlerCount} handlers, processed {itemCount} items, {changeCount} changes in {ElapsedMilliseconds}ms",
                handlers.Count(), actions.Count, actions.Where(x => x.Change > Core.ChangeType.NoChange).Count(),
                sw.ElapsedMilliseconds);

            callbacks?.Update?.Invoke($"Processed {actions.Count} items in {sw.ElapsedMilliseconds}ms", 1, 1);

            return actions;
        }

        #endregion

        #region Importing
        private static object _importLock = new object();

        /// <summary>
        ///  Import items into umbraco from a given folder
        /// </summary>
        /// <param name="folder">Folder to use for the import</param>
        /// <param name="force">Push changes in even if there is no difference between the file and the item in umbraco</param>
        /// <param name="handlerOptions">Handler options to use (used to calculate handlers to use)</param>
        /// <param name="callbacks">Callbacks to keep UI informed</param>
        /// <returns>List of actions detailing what did and didn't change</returns>
        public IEnumerable<uSyncAction> Import(string folder, bool force, SyncHandlerOptions handlerOptions, uSyncCallbacks callbacks = null)
        {
            if (handlerOptions == null) handlerOptions = new SyncHandlerOptions();
            handlerOptions.Action = HandlerActions.Import;

            var handlers = handlerFactory.GetValidHandlers(handlerOptions);
            return Import(folder, force, handlers, callbacks);
        }

        /// <summary>
        ///  Import items into umbraco from a given folder
        /// </summary>
        /// <param name="folder">Folder to use for the import</param>
        /// <param name="force">Push changes in even if there is no difference between the file and the item in umbraco</param>
        /// <param name="handlerAliases">List of aliases for the handlers you want to use</param>
        /// <param name="callbacks">Callbacks to keep UI informed</param>
        /// <returns>List of actions detailing what did and didn't change</returns>
        public IEnumerable<uSyncAction> Import(string folder, bool force, IEnumerable<string> handlerAliases, uSyncCallbacks callbacks)
        {
            var handlers = handlerFactory.GetDefaultHandlers(handlerAliases);
            return Import(folder, force, handlers, callbacks);
        }

        /// <summary>
        ///  Import items into umbraco from a given folder
        /// </summary>
        /// <param name="folder">Folder to use for the import</param>
        /// <param name="force">Push changes in even if there is no difference between the file and the item in umbraco</param>
        /// <param name="handlers">List of Handlers & config to use for import</param>
        /// <param name="callbacks">Callbacks to keep UI informed</param>
        /// <returns>List of actions detailing what did and didn't change</returns>
        public IEnumerable<uSyncAction> Import(string folder, bool force, IEnumerable<ExtendedHandlerConfigPair> handlers, uSyncCallbacks callbacks)
        {
            // if its blank, we just throw it back empty. 
            if (handlers == null || !handlers.Any()) return Enumerable.Empty<uSyncAction>();

            lock (_importLock)
            {
                var sw = Stopwatch.StartNew();

                try
                {
                    uSync8BackOffice.eventsPaused = true;

                    // pre import event
                    fireBulkStarting(ImportStarting);

                    var actions = new List<uSyncAction>();

                    var summary = new SyncProgressSummary(handlers.Select(x => x.Handler), "Importing", handlers.Count() + 1);
                    summary.Handlers.Add(new SyncHandlerSummary()
                    {
                        Icon = "icon-traffic",
                        Name = "Post Import",
                        Status = HandlerStatus.Pending
                    });

                    foreach (var configuredHandler in handlers)
                    {
                        var handler = configuredHandler.Handler;
                        var handlerSettings = configuredHandler.Settings;

                        summary.Count++;

                        summary.UpdateHandler(
                            handler.Name, HandlerStatus.Processing, $"Importing {handler.Name}", 0);

                        callbacks?.Callback?.Invoke(summary);

                        var handlerActions = handler.ImportAll($"{folder}/{handler.DefaultFolder}", handlerSettings, force, callbacks?.Update);
                        actions.AddRange(handlerActions);

                        summary.UpdateHandler(handler.Name, HandlerStatus.Complete, ChangeCount(handlerActions), ContainsErrors(handlerActions));
                    }


                    // postImport things (mainly cleaning up folders)

                    summary.Count++;
                    summary.UpdateHandler("Post Import", HandlerStatus.Pending, "Post Import Actions", 0);

                    callbacks?.Callback?.Invoke(summary);

                    var postImportActions = actions.Where(x => x.Success
                                                && x.Change > Core.ChangeType.NoChange
                                                && x.RequiresPostProcessing);

                    foreach (var configuredHandler in handlers)
                    {
                        var handler = configuredHandler.Handler;
                        var handlerSettings = configuredHandler.Settings;

                        if (handler is ISyncPostImportHandler postHandler)
                        {
                            var handlerActions = postImportActions.Where(x => x.ItemType == handler.ItemType);

                            if (handlerActions.Any())
                            {
                                var postActions = postHandler.ProcessPostImport($"{folder}/{handler.DefaultFolder}", handlerActions, handlerSettings);
                                if (postActions != null)
                                    actions.AddRange(postActions);
                            }
                        }
                    }

                    sw.Stop();
                    summary.UpdateHandler("Post Import", HandlerStatus.Complete, "Import Completed", 0);
                    callbacks?.Callback?.Invoke(summary);

                    // fire complete
                    fireBulkComplete(ImportComplete, actions);

                    logger.Info<uSyncService>("uSync Import: {handlerCount} handlers, processed {itemCount} items, {changeCount} changes in {ElapsedMilliseconds}ms",
                        handlers.Count(), actions.Count, actions.Where(x => x.Change > Core.ChangeType.NoChange).Count(),
                        sw.ElapsedMilliseconds);

                    callbacks?.Update?.Invoke($"Processed {actions.Count} items in {sw.ElapsedMilliseconds}ms", 1, 1);

                    return actions;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    uSync8BackOffice.eventsPaused = false;
                }
            }
        }


        /// <summary>
        ///  Import a single item based on a uSyncAction item
        /// </summary>
        /// <remarks>
        ///  Importing a single item based on an action, the action will
        ///  detail what handler to use and the filename of the item to import
        /// </remarks>
        /// <param name="action">Action item, to use as basis for import</param>
        /// <returns>Action detailing change or not</returns>
        public uSyncAction ImportSingleAction(uSyncAction action)
        {
            var handlerConfig = handlerFactory.GetValidHandler(action.HandlerAlias);
            if (handlerConfig != null && handlerConfig.Handler is ISyncExtendedHandler extendedHandler)
            {
                extendedHandler.Import(action.FileName, handlerConfig.Settings, true);
            }

            return new uSyncAction();

        }

        #endregion

        #region Exporting 


        public bool CleanExportFolder(string folder)
        {
            try
            {
                if (syncFileService.DirectoryExists(folder))
                    syncFileService.CleanFolder(folder);
            }
            catch(Exception ex)
            {
                throw new ApplicationException("Failed to delete uSync folder (may be in use)", ex);
            }

            return true;
        }


        /// <summary>
        ///  Export items from umbraco into a given folder
        /// </summary>
        /// <param name="folder">folder to place items</param>
        /// <param name="handlerOptions">Handler options to use when loading handlers</param>
        /// <param name="callbacks">callback functions to update the UI</param>
        /// <returns>List of actions detailing what was exported</returns>
        public IEnumerable<uSyncAction> Export(string folder, SyncHandlerOptions handlerOptions, uSyncCallbacks callbacks = null)
        {
            if (handlerOptions == null) handlerOptions = new SyncHandlerOptions();
            handlerOptions.Action = HandlerActions.Export;

            var handlers = handlerFactory.GetValidHandlers(handlerOptions);
            return Export(folder, handlers, callbacks);
        }

        /// <summary>
        ///  Export items from umbraco into a given folder
        /// </summary>
        /// <param name="folder">folder to place items</param>
        /// <param name="handlerAliases">aliases for the handlers to use while exporting</param>
        /// <param name="callbacks">callback functions to update the UI</param>
        /// <returns>List of actions detailing what was exported</returns>
        public IEnumerable<uSyncAction> Export(string folder, IEnumerable<string> handlerAliases, uSyncCallbacks callbacks)
        {
            var handlers = handlerFactory.GetDefaultHandlers(handlerAliases);
            return Export(folder, handlers, callbacks);
        }

        /// <summary>
        ///  Export items from umbraco into a given folder
        /// </summary>
        /// <param name="folder">folder to place items</param>
        /// <param name="handlerAliases">List of handlers to use for export</param>
        /// <param name="callbacks">callback functions to update the UI</param>
        /// <returns>List of actions detailing what was exported</returns>
        public IEnumerable<uSyncAction> Export(string folder, IEnumerable<ExtendedHandlerConfigPair> handlers, uSyncCallbacks callbacks)
        {
            var sw = Stopwatch.StartNew();

            fireBulkStarting(ExportStarting);

            var actions = new List<uSyncAction>();
            var summary = new SyncProgressSummary(handlers.Select(x => x.Handler), "Exporting", handlers.Count());

            foreach (var configuredHandler in handlers)
            {
                var handler = configuredHandler.Handler;
                summary.Count++;

                summary.UpdateHandler(
                    handler.Name, HandlerStatus.Processing, $"Exporting {handler.Name}", 0);

                callbacks?.Callback?.Invoke(summary);

                var handlerActions = handler.ExportAll($"{folder}/{handler.DefaultFolder}", configuredHandler.Settings, callbacks?.Update);

                actions.AddRange(handlerActions);

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete, ChangeCount(handlerActions), ContainsErrors(handlerActions));
            }


            summary.Message = "Export Completed";
            callbacks?.Callback?.Invoke(summary);

            fireBulkComplete(ExportComplete, actions);

            sw.Stop();

            logger.Info<uSyncService>("uSync Export: {handlerCount} handlers, processed {itemCount} items, {changeCount} changes in {ElapsedMilliseconds}ms",
                handlers.Count(), actions.Count, actions.Where(x => x.Change > Core.ChangeType.NoChange).Count(),
                sw.ElapsedMilliseconds);

            callbacks?.Update?.Invoke($"Processed {actions.Count} items in {sw.ElapsedMilliseconds}ms", 1, 1);

            return actions;
        }

        #endregion

        #region Obsolete calls (callback, update)

        // v8.1 calls

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Import(string folder, bool force, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Import(folder, force, default(SyncHandlerOptions), new uSyncCallbacks(callback, update));

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Report(string folder, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Report(folder, default(SyncHandlerOptions), new uSyncCallbacks(callback, update));

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Export(string folder, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Export(folder, default(SyncHandlerOptions), new uSyncCallbacks(callback, update));

        #endregion

        /// <summary>
        ///  calculate the number of actions in a list that are actually changes
        /// </summary>
        /// <param name="actions">List of actions to parse</param>
        /// <returns>number of actions that contain a change of some form</returns>
        private int ChangeCount(IEnumerable<uSyncAction> actions)
            => actions.Count(x => x.Change > Core.ChangeType.NoChange);

        private bool ContainsErrors(IEnumerable<uSyncAction> actions)
            => actions.Any(x => x.Change >= Core.ChangeType.Fail);

        /// <summary>
        ///  Do an import triggered by an event.
        /// </summary>
        /// <param name="e"></param>
        private void USyncTriggers_DoImport(uSyncTriggerArgs e)
        {
            if (e.EntityTypes != null && !string.IsNullOrWhiteSpace(e.Folder))
            {
                logger.Info<uSyncService>("Import Triggered by downlevel change {0}", e.Folder);

                var handlers = GetHandlersByEntitytype(e.EntityTypes, e.HandlerOptions);
                if (handlers.Count > 0)
                    this.Import(e.Folder, false, handlers, null);
            }
        }

        /// <summary>
        ///  do an export triggered by events. 
        /// </summary>
        /// <param name="e"></param>
        private void USyncTriggers_DoExport(uSyncTriggerArgs e)
        {
            if (e.EntityTypes != null && !string.IsNullOrWhiteSpace(e.Folder))
            {
                logger.Info<uSyncService>("Export Triggered by downlevel change {0}", e.Folder);
                var handlers = GetHandlersByEntitytype(e.EntityTypes, e.HandlerOptions);
                if (handlers.Count > 0)
                {
                    this.Export(e.Folder, handlers, null);
                }
            }
        }

        /// <summary>
        ///  Get a list of handlers for a set of entity types.
        /// </summary>
        /// <param name="entityTypes">Entity types to find handlers for</param>
        /// <param name="handlerOptions">Options to use when loading the handlers</param>
        /// <returns>List of Handler/Config pairs for handlers for entity types</returns>
        private IList<ExtendedHandlerConfigPair> GetHandlersByEntitytype(IEnumerable<string> entityTypes, SyncHandlerOptions handlerOptions)
        {
            var handlers = new List<ExtendedHandlerConfigPair>();

            foreach (var entityType in entityTypes)
            {
                var handler = handlerFactory.GetValidHandlerByEntityType(entityType, handlerOptions);
                if (handler != null)
                    handlers.Add(handler);
            }

            return handlers;
        }

    }
}
