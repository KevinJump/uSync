using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
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
        private readonly uSyncBackOfficeSettings settings;
        private readonly SyncHandlerCollection syncHandlers;

        public uSyncService(            
            SyncHandlerCollection syncHandlers,
            uSyncBackOfficeSettings settings)
        {
            this.syncHandlers = syncHandlers;
            this.settings = settings;
        }

        public delegate void SyncEventCallback(SyncProgressSummary summary);

        public IEnumerable<uSyncAction> Report(string folder, SyncEventCallback callback = null)
        {
            var actions = new List<uSyncAction>();

            var handlers = syncHandlers.Where(x => x.Enabled).ToList();

            var summary = new SyncProgressSummary(handlers, "Reporting", handlers.Count);
            
            foreach (var handler in handlers)
            {
                summary.Processed++;

                summary.UpdateHandler(
                    handler.Name, HandlerStatus.Processing, $"Reporting {handler.Name}");

                callback?.Invoke(summary);

                actions.AddRange(handler.Report($"{folder}/{handler.DefaultFolder}"));

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete);
            }

            summary.Message = "Report Complete";
            callback?.Invoke(summary);

            return actions;
        }

        public IEnumerable<uSyncAction> Import(string folder, bool force, SyncEventCallback callback = null)
        {
            var actions = new List<uSyncAction>();

            var handlers = syncHandlers.Where(x => x.Enabled).ToList();
            var summary = new SyncProgressSummary(handlers, "Importing", handlers.Count + 1);
            summary.Handlers.Add(new SyncHandlerSummary()
            {
                Icon = "icon-traffic",
                Name = "Post Import",
                Status = HandlerStatus.Pending
            });

            foreach (var handler in handlers)
            {
                summary.Processed++;

                summary.UpdateHandler(
                    handler.Name, HandlerStatus.Processing, $"Importing {handler.Name}");

                callback?.Invoke(summary);

                actions.AddRange(handler.ImportAll($"{folder}/{handler.DefaultFolder}", force));

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete);
            }


            // postImport things (mainly cleaning up folders)

            summary.Processed++;
            summary.UpdateHandler("Post Import", HandlerStatus.Pending, "Post Import Actions");

            callback?.Invoke(summary);

            var postImportActions = actions.Where(x => x.Success
                                        && x.Change > Core.ChangeType.NoChange
                                        && x.RequiresPostProcessing);

            foreach(var handler in handlers)
            {
                if (handler is ISyncPostImportHandler postHandler)
                {
                    var handlerActions = postImportActions.Where(x => x.ItemType == handler.ItemType);

                    if (handlerActions.Any())
                    {
                        var postActions = postHandler.ProcessPostImport($"{folder}/{handler.DefaultFolder}", handlerActions);
                        if (postActions != null)
                            actions.AddRange(postActions);
                    }
                }
            }

            summary.UpdateHandler("Post Import", HandlerStatus.Complete, "Import Completed");
            callback?.Invoke(summary);

            return actions;
        }

        public IEnumerable<uSyncAction> Export(string folder, SyncEventCallback callback = null)
        {
            var actions = new List<uSyncAction>();

            var handlers = syncHandlers.Where(x => x.Enabled).ToList();
            var summary = new SyncProgressSummary(handlers, "Exporting", handlers.Count);

            foreach (var handler in handlers)
            {
                summary.Processed++;

                summary.UpdateHandler(
                    handler.Name, HandlerStatus.Processing, $"Exporting {handler.Name}");

                callback?.Invoke(summary);

                actions.AddRange(handler.ExportAll($"{folder}/{handler.DefaultFolder}"));

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete);
            }

            summary.Message = "Export Completed";
            callback?.Invoke(summary);

            return actions;

        }

    }

    public class SyncProgressSummary
    {
        public int Processed { get; set; }
        public int TotalSteps { get; set; }
        public string Message { get; set; }
        public List<SyncHandlerSummary> Handlers { get; set; }

        public SyncProgressSummary(
            IEnumerable<ISyncHandler> handlers, 
            string message,
            int totalSteps)
        {
            this.TotalSteps = totalSteps;
            this.Message = message;

            this.Handlers = handlers.Select(x => new SyncHandlerSummary()
            {
                Icon = x.Icon,
                Name = x.Name,
                Status = HandlerStatus.Pending
            }).ToList();
        }

        public void UpdateHandler(string name, HandlerStatus status)
        {
            var item = this.Handlers.FirstOrDefault(x => x.Name == name);
            if (item != null)
                item.Status = status;
        }

        public void UpdateHandler(string name, HandlerStatus status, string message)
        {
            UpdateHandler(name, status);
            this.Message = message;
        }

    }

    public class SyncHandlerSummary
    {
        public string Icon { get; set; }
        public string Name { get; set; }
        public HandlerStatus Status { get; set; }
    }

    public enum HandlerStatus
    {
        Pending, 
        Processing,
        Complete,
        Error
    }
}
