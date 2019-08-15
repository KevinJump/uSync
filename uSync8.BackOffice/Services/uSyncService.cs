using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Umbraco.Core.Composing;
using Umbraco.Core.Logging;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.SyncHandlers;

using static uSync8.BackOffice.uSyncService;

namespace uSync8.BackOffice
{
    public class uSyncCallbacks
    {
        public SyncEventCallback Callback { get; private set; }
        public SyncUpdateCallback Update { get; private set; }

        public uSyncCallbacks(SyncEventCallback callback, SyncUpdateCallback update)
        {
            this.Callback = callback;
            this.Update = update;
        }
    }

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
        public IEnumerable<uSyncAction> Report(string folder, uSyncCallbacks callbacks)
            => Report(folder, uSync.Handlers.DefaultSet, string.Empty, callbacks);

        public IEnumerable<uSyncAction> Report(string folder, string group, uSyncCallbacks callbacks)
            => Report(folder, uSync.Handlers.DefaultSet, group, callbacks);

        public IEnumerable<uSyncAction> Report(string folder, string handlerSet, string group, uSyncCallbacks callbacks)
        {
            var handlers = handlerFactory.GetValidHandlers(handlerSet, group, HandlerActionNames.Report);
            return Report(folder, handlers, callbacks);
        }

        private IEnumerable<uSyncAction> Report(string folder, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks callbacks)
        {
            logger.Debug<uSyncService>("Reporting For [{0}]", string.Join(",", handlers.Select(x => x.Handler.Name)));

            var actions = new List<uSyncAction>();

            var summary = new SyncProgressSummary(handlers.Select(x => x.Handler), "Reporting", handlers.Count());

            if (settings.ReportDebug)
            {
                // debug - full export into a dated folder. 
                summary.Message = "Debug: Creating Extract in Tracker folder";
                callbacks?.Callback?.Invoke(summary);
                this.Export($"~/uSync/Tracker/{DateTime.Now.ToString("yyyyMMdd_HHmmss")}/", null);
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

        public IEnumerable<uSyncAction> Import(string folder, bool force, uSyncCallbacks callbacks)
            => Import(folder, force, string.Empty, callbacks);

        public IEnumerable<uSyncAction> Import(string folder, bool force, string groupName, uSyncCallbacks callbacks)
            => Import(folder, force, uSync.Handlers.DefaultSet, groupName, callbacks);

        public IEnumerable<uSyncAction> Import(string folder, bool force, string setName, string groupName, uSyncCallbacks callbacks)
        {
            var handlers = handlerFactory.GetValidHandlers(setName, groupName, HandlerActionNames.Import);
            return Import(folder, force, handlers, callbacks);
        }

        private IEnumerable<uSyncAction> Import(string folder, bool force, IEnumerable<HandlerConfigPair> handlers,  uSyncCallbacks callbacks)
        {
            lock (_importLock)
            {
                var sw = Stopwatch.StartNew();

                try
                {
                    uSync8BackOffice.eventsPaused = true;

                    var actions = new List<uSyncAction>();

                    // var configuredHandlers = syncHandlers.GetValidHandlers("import", settings).ToList();

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
            var handlerConfig = handlerFactory.GetValidHandler(action.HandlerAlias, uSync.Handlers.DefaultSet);
            if (handlerConfig != null && handlerConfig.Handler is ISyncExtendedHandler handler2)
            {
                handler2.Import(action.FileName, handlerConfig.Settings, true);
            }

            return new uSyncAction();

        }

        #endregion

        #region Exporting 
        public IEnumerable<uSyncAction> Export(string folder, uSyncCallbacks callbacks)
            => Export(folder, string.Empty, callbacks);

        public IEnumerable<uSyncAction> Export(string folder, string groupName, uSyncCallbacks callbacks)
            => Export(folder, uSync.Handlers.DefaultSet, groupName, callbacks);

        public IEnumerable<uSyncAction> Export(string folder, string setName, string groupName, uSyncCallbacks callbacks)
        {
            var handlers = handlerFactory.GetValidHandlers(setName, groupName, HandlerActionNames.Export);
            return Export(folder, handlers, callbacks);
        }

        private IEnumerable<uSyncAction> Export(string folder, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks callbacks)
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
            => Import(folder, force, new uSyncCallbacks(callback, update));

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Report(string folder, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Report(folder, new uSyncCallbacks(callback, update));

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Export(string folder, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Export(folder, new uSyncCallbacks(callback, update));

        #endregion

        private int ChangeCount(IEnumerable<uSyncAction> actions)
            => actions.Count(x => x.Change > Core.ChangeType.NoChange);
    }
}
