using System;
using System.Collections.Generic;
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

        public IEnumerable<uSyncAction> Report(string folder, SyncEventCallback callback = null)
        {
            var actions = new List<uSyncAction>();

            var configuredHandlers = syncHandlers.GetValidHandlers("report", settings).ToList();

            var summary = new SyncProgressSummary(configuredHandlers.Select(x => x.Handler), "Reporting", configuredHandlers.Count);

            foreach (var configuredHandler in configuredHandlers)
            {
                var handler = configuredHandler.Handler;
                var handlerSettings = configuredHandler.Settings;

                summary.Processed++;

                summary.UpdateHandler(handler.Name, HandlerStatus.Processing, $"Reporting {handler.Name}");

                callback?.Invoke(summary);

                actions.AddRange(handler.Report($"{folder}/{handler.DefaultFolder}", handlerSettings));

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete);
            }

            summary.Message = "Report Complete";
            callback?.Invoke(summary);

            return actions;
        }

        private static object _importLock = new object();

        public IEnumerable<uSyncAction> Import(string folder, bool force, SyncEventCallback callback = null)
        {
            lock (_importLock)
            {
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

                        summary.Processed++;

                        summary.UpdateHandler(
                            handler.Name, HandlerStatus.Processing, $"Importing {handler.Name}");

                        callback?.Invoke(summary);

                        actions.AddRange(handler.ImportAll($"{folder}/{handler.DefaultFolder}", handlerSettings, force));

                        summary.UpdateHandler(handler.Name, HandlerStatus.Complete);
                    }


                    // postImport things (mainly cleaning up folders)

                    summary.Processed++;
                    summary.UpdateHandler("Post Import", HandlerStatus.Pending, "Post Import Actions");

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

                    summary.UpdateHandler("Post Import", HandlerStatus.Complete, "Import Completed");
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

        public IEnumerable<uSyncAction> Export(string folder, SyncEventCallback callback = null)
        {
            var actions = new List<uSyncAction>();

            var configuredHandlers = syncHandlers.GetValidHandlers("export", settings).ToList();

            var summary = new SyncProgressSummary(configuredHandlers.Select(x => x.Handler), "Exporting", configuredHandlers.Count);

            foreach (var configuredHandler in configuredHandlers)
            {
                var handler = configuredHandler.Handler;
                summary.Processed++;

                summary.UpdateHandler(
                    handler.Name, HandlerStatus.Processing, $"Exporting {handler.Name}");

                callback?.Invoke(summary);

                actions.AddRange(handler.ExportAll($"{folder}/{handler.DefaultFolder}", configuredHandler.Settings));

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete);
            }

            summary.Message = "Export Completed";
            callback?.Invoke(summary);

            return actions;

        }
    }
   
}
