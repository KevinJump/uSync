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
        private uSyncSettings settings;
        private readonly SyncHandlerCollection syncHandlers;

        public uSyncService(
            SyncHandlerCollection syncHandlers
            )
        {
            this.syncHandlers = syncHandlers;
            this.settings = Current.Configs.uSync();

            uSyncConfig.Reloaded += BackOfficeConfig_Reloaded;
        }

        private void BackOfficeConfig_Reloaded(uSyncSettings settings)
        {
            this.settings = Current.Configs.uSync();
        }

        public delegate void SyncEventCallback(SyncProgressSummary summary);

        public IEnumerable<uSyncAction> Report(string folder, SyncEventCallback callback = null, SyncUpdateCallback update = null)
        {
            var actions = new List<uSyncAction>();

            var configuredHandlers = syncHandlers.GetValidHandlers("report", settings).ToList();

            var summary = new SyncProgressSummary(configuredHandlers.Select(x => x.Handler), "Reporting", configuredHandlers.Count);

            if (settings.ReportDebug)
            {
                // debug - full export into a dated folder. 
                summary.Message = "Debug: Creating Extract in Tracker folder";
                callback?.Invoke(summary);
                this.Export($"~/uSync/Tracker/{DateTime.Now.ToString("yyyyMMdd_HHmmss")}/", null, null);
            }

            foreach (var configuredHandler in configuredHandlers)
            {
                var handler = configuredHandler.Handler;
                var handlerSettings = configuredHandler.Settings;

                summary.Count++;

                summary.UpdateHandler(handler.Name, HandlerStatus.Processing, $"Reporting {handler.Name}", 0);

                callback?.Invoke(summary);

                var handlerActions = handler.Report($"{folder}/{handler.DefaultFolder}", handlerSettings, update);
                actions.AddRange(handlerActions);

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete, ChangeCount(handlerActions));
            }

            summary.Message = "Report Complete";
            callback?.Invoke(summary);

            return actions;
        }

        private static object _importLock = new object();

        public IEnumerable<uSyncAction> Import(string folder, bool force, SyncEventCallback callback = null, SyncUpdateCallback update = null)
        {
            lock (_importLock)
            {
                var sw = Stopwatch.StartNew();

                try
                {
                    uSync8BackOffice.eventsPaused = true;

                    var actions = new List<uSyncAction>();

                    var configuredHandlers = syncHandlers.GetValidHandlers("import", settings).ToList();

                    var summary = new SyncProgressSummary(configuredHandlers.Select(x => x.Handler), "Importing", configuredHandlers.Count + 1);
                    summary.Handlers.Add(new SyncHandlerSummary()
                    {
                        Icon = "icon-traffic",
                        Name = "Post Import",
                        Status = HandlerStatus.Pending
                    });

                    foreach (var configuredHandler in configuredHandlers)
                    {
                        var handler = configuredHandler.Handler;
                        var handlerSettings = configuredHandler.Settings;

                        summary.Count++;

                        summary.UpdateHandler(
                            handler.Name, HandlerStatus.Processing, $"Importing {handler.Name}", 0);

                        callback?.Invoke(summary);

                        var handlerActions = handler.ImportAll($"{folder}/{handler.DefaultFolder}", handlerSettings, force, update);
                        actions.AddRange(handlerActions);

                        summary.UpdateHandler(handler.Name, HandlerStatus.Complete, ChangeCount(handlerActions));
                    }


                    // postImport things (mainly cleaning up folders)

                    summary.Count++;
                    summary.UpdateHandler("Post Import", HandlerStatus.Pending, "Post Import Actions", 0);

                    callback?.Invoke(summary);

                    var postImportActions = actions.Where(x => x.Success
                                                && x.Change > Core.ChangeType.NoChange
                                                && x.RequiresPostProcessing);

                    foreach (var configuredHandler in configuredHandlers)
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
                    callback?.Invoke(summary);

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
            var configuredHandlers = syncHandlers.GetValidHandlers("import", settings).ToList();

            var handler = configuredHandlers.FirstOrDefault(x => x.Handler.Alias == action.HandlerAlias);
            if (handler != null)
            {
                // handler.Handler.ImportAll(action.FileName);
            }

            return new uSyncAction();

        }

        public IEnumerable<uSyncAction> Export(string folder, SyncEventCallback callback = null, SyncUpdateCallback update = null)
        {
            var actions = new List<uSyncAction>();

            var configuredHandlers = syncHandlers.GetValidHandlers("export", settings).ToList();

            var summary = new SyncProgressSummary(configuredHandlers.Select(x => x.Handler), "Exporting", configuredHandlers.Count);

            foreach (var configuredHandler in configuredHandlers)
            {
                var handler = configuredHandler.Handler;
                summary.Count++;

                summary.UpdateHandler(
                    handler.Name, HandlerStatus.Processing, $"Exporting {handler.Name}", 0);

                callback?.Invoke(summary);

                var handlerActions = handler.ExportAll($"{folder}/{handler.DefaultFolder}", configuredHandler.Settings, update);

                actions.AddRange(handlerActions);

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete, ChangeCount(handlerActions));
            }


            summary.Message = "Export Completed";
            callback?.Invoke(summary);

            return actions;

        }

        private int ChangeCount(IEnumerable<uSyncAction> actions)
        {
            return actions.Count(x => x.Change > Core.ChangeType.NoChange);
        }
    }
   
}
