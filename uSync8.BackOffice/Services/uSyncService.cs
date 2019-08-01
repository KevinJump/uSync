using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private readonly SyncHandlerCollection syncHandlers;
        private readonly IProfilingLogger logger;

        public uSyncService(
            SyncHandlerCollection syncHandlers,
            IProfilingLogger logger)
        {
            this.syncHandlers = syncHandlers;
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
        {
            var configuredHandlers = syncHandlers.GetValidHandlers(HandlerActionNames.Report, settings).ToList();
            return Report(folder, configuredHandlers, callbacks);
        }

        public IEnumerable<uSyncAction> Report(string folder, string handlerGroup, uSyncCallbacks callbacks)
        {
            var configuredHandlers = syncHandlers.GetValidHandlers(HandlerActionNames.Report, handlerGroup, settings);
            return Report(folder, configuredHandlers, callbacks);
        }
        public IEnumerable<uSyncAction> Report(string folder, IEnumerable<string> handlerTypes, uSyncCallbacks callbacks)
        {
            var handlers = syncHandlers.GetHandlersByType(handlerTypes, HandlerActionNames.Report, settings);
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
        {
            var handlers = syncHandlers.GetValidHandlers(HandlerActionNames.Import, settings).ToList();
            return Import(folder, force, handlers, callbacks);

        }

        public IEnumerable<uSyncAction> Import(string folder, bool force, IEnumerable<string> handlerTypes, uSyncCallbacks callbacks)
        {
            var handlers = syncHandlers.GetHandlersByType(handlerTypes, HandlerActionNames.Import, settings);
            return Import(folder, force, handlers, callbacks);
        }

        public IEnumerable<uSyncAction> Import(string folder, bool force, string groupName, uSyncCallbacks callbacks)
        {
            var handlers = syncHandlers.GetValidHandlers(HandlerActionNames.Import, groupName, settings);
            return Import(folder, force, handlers, callbacks);
        }

        private IEnumerable<uSyncAction> Import(string folder, bool force, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks callbacks)
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

        public uSyncAction Import(uSyncAction action)
        {
            var configuredHandlers = syncHandlers.GetValidHandlers(HandlerActionNames.Import, settings).ToList();

            var handlerConfig = configuredHandlers.FirstOrDefault(x => x.Handler.Alias == action.HandlerAlias);
            if (handlerConfig != null && handlerConfig.Handler is ISyncSingleItemHandler handler2)
            {
                handler2.Import(action.FileName, handlerConfig.Settings, true);
            }

            return new uSyncAction();

        }

        #endregion

        #region Exporting 
        public IEnumerable<uSyncAction> Export(string folder, uSyncCallbacks callbacks)
        {
            var configuredHandlers = syncHandlers.GetValidHandlers(HandlerActionNames.Export, settings).ToList();
            return Export(folder, configuredHandlers, callbacks);
        }

        public IEnumerable<uSyncAction> Export(string folder, IEnumerable<string> handlerTypes, uSyncCallbacks callbacks)
        {
            var handlers = syncHandlers.GetHandlersByType(handlerTypes, HandlerActionNames.Export, settings);
            return Export(folder, handlers, callbacks);
        }

        public IEnumerable<uSyncAction> Export(string folder, string groupName, uSyncCallbacks callbacks)
        {
            var handlers = syncHandlers.GetValidHandlers(HandlerActionNames.Export, groupName, settings);
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
            callbacks.Callback?.Invoke(summary);

            return actions;
        }

        #endregion

        #region Obsolete calls (callback, update)

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Import(string folder, bool force, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Import(folder, force, new uSyncCallbacks(callback, update));

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Import(string folder, bool force, IEnumerable<string> handlerTypes, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Import(folder, force, handlerTypes, new uSyncCallbacks(callback, update));

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Import(string folder, bool force, string groupName, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Import(folder, force, groupName, new uSyncCallbacks(callback, update));


        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Report(string folder, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Report(folder, new uSyncCallbacks(callback, update));

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Report(string folder, string handlerGroup, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Report(folder, handlerGroup, new uSyncCallbacks(callback, update));

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Report(string folder, IEnumerable<string> handlerTypes, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Report(folder, handlerTypes, new uSyncCallbacks(callback, update));


        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Export(string folder, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Export(folder, new uSyncCallbacks(callback, update));

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Export(string folder, IEnumerable<string> handlerTypes, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Export(folder, handlerTypes, new uSyncCallbacks(callback, update));

        [Obsolete("Use the new uSyncCallbacks group when calling")]
        public IEnumerable<uSyncAction> Export(string folder, string groupName, SyncEventCallback callback = null, SyncUpdateCallback update = null)
            => Export(folder, groupName, new uSyncCallbacks(callback, update));

        #endregion

        private int ChangeCount(IEnumerable<uSyncAction> actions)
        {
            return actions.Count(x => x.Change > Core.ChangeType.NoChange);
        }
    }

}
