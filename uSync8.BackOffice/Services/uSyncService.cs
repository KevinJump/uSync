using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Umbraco.Core.Composing;
using Umbraco.Core.Logging;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice
{

    /// <summary>
    ///  the service that does all the processing,
    ///  this forms the entry point as an API to 
    ///  uSync, it is where imports, exports and reports
    ///  are actually ran from. 
    /// </summary>
    public class uSyncService
    {
        public delegate void SyncEventCallback(SyncProgressSummary summary);

        private uSyncSettings settings;
        private readonly SyncHandlerFactory handlerFactory;
        private readonly IProfilingLogger logger;

        public uSyncService(
            SyncHandlerFactory handlerFactory,
            IProfilingLogger logger)
        {
            this.handlerFactory = handlerFactory;

            this.settings = Current.Configs.uSync();
            this.logger = logger;

            uSyncConfig.Reloaded += BackOfficeConfig_Reloaded;
        }

        private void BackOfficeConfig_Reloaded(uSyncSettings settings)
        {
            this.settings = Current.Configs.uSync();
        }

        #region Reporting 

        public IEnumerable<uSyncAction> Report(string folder, SyncHandlerOptions handlerOptions , uSyncCallbacks callbacks = null)
        {
            if (handlerOptions == null) handlerOptions = new SyncHandlerOptions();
            handlerOptions.Action = HandlerActions.Report;

            var handlers = handlerFactory.GetValidHandlers(handlerOptions);
            return Report(folder, handlers, callbacks);
        }

        public IEnumerable<uSyncAction> Report(string folder, IEnumerable<string> handlerAliases, uSyncCallbacks callbacks)
        {
            var handlers = handlerFactory.GetDefaultHandlers(handlerAliases);
            return Report(folder, handlers, callbacks);
        }

        public IEnumerable<uSyncAction> Report(string folder, IEnumerable<ExtendedHandlerConfigPair> handlers, uSyncCallbacks callbacks)
        {
            logger.Debug<uSyncService>("Reporting For [{0}]", string.Join(",", handlers.Select(x => x.Handler.Name)));

            var actions = new List<uSyncAction>();

            var summary = new SyncProgressSummary(handlers.Select(x => x.Handler), "Reporting", handlers.Count());

            if (settings.ReportDebug)
            {
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

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete, ChangeCount(handlerActions));
            }

            summary.Message = "Report Complete";
            callbacks?.Callback?.Invoke(summary);

            return actions;
        }

        #endregion

        #region Importing
        private static object _importLock = new object();

        public IEnumerable<uSyncAction> Import(string folder, bool force, SyncHandlerOptions handlerOptions, uSyncCallbacks callbacks = null)
        {
            if (handlerOptions == null) handlerOptions = new SyncHandlerOptions();
            handlerOptions.Action = HandlerActions.Import;

            var handlers = handlerFactory.GetValidHandlers(handlerOptions);
            return Import(folder, force, handlers, callbacks);
        }

        /// <summary>
        ///  Import using the speicifed handlers - regardless of config settings 
        /// </summary>
        /// <remarks>
        ///  When importing, with aliases, unless we say so - we ignore the sets, because we know what we want 
        /// </remarks>
        /// <returns>
        ///  Collection of uSyncActions detailed what imports took place. 
        /// </returns>
        public IEnumerable<uSyncAction> Import(string folder, bool force, IEnumerable<string> handlerAliases, uSyncCallbacks callbacks)
        { 
            var handlers = handlerFactory.GetDefaultHandlers(handlerAliases);
            return Import(folder, force, handlers, callbacks);
        }

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

                        summary.UpdateHandler(handler.Name, HandlerStatus.Complete, ChangeCount(handlerActions));
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
                    summary.UpdateHandler("Post Import", HandlerStatus.Complete,
                        $"Import Completed ({sw.ElapsedMilliseconds}ms)", 0);
                    callbacks?.Callback?.Invoke(summary);

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

        public IEnumerable<uSyncAction> Export(string folder, SyncHandlerOptions handlerOptions,  uSyncCallbacks callbacks = null)
        {
            if (handlerOptions == null) handlerOptions = new SyncHandlerOptions();
            handlerOptions.Action = HandlerActions.Export;

            var handlers = handlerFactory.GetValidHandlers(handlerOptions);
            return Export(folder, handlers, callbacks);
        }

        public IEnumerable<uSyncAction> Export(string folder, IEnumerable<string> handlerAliases, uSyncCallbacks callbacks)
        {
            var handlers = handlerFactory.GetDefaultHandlers(handlerAliases);
            return Export(folder, handlers , callbacks);
        }

        public IEnumerable<uSyncAction> Export(string folder, IEnumerable<ExtendedHandlerConfigPair> handlers, uSyncCallbacks callbacks)
        {
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

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete, ChangeCount(handlerActions));
            }


            summary.Message = "Export Completed";
            callbacks?.Callback?.Invoke(summary);

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

        private int ChangeCount(IEnumerable<uSyncAction> actions)
            => actions.Count(x => x.Change > Core.ChangeType.NoChange);
    }
}
