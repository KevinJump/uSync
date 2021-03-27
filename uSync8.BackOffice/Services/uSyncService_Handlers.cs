using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uSync8.BackOffice.SyncHandlers;
using uSync8.Core.Extensions;
using uSync8.Core.Serialization;

namespace uSync8.BackOffice
{
    /// <summary>
    ///  actions on individual handlers. 
    /// </summary>

    public partial class uSyncService
    {

        public IEnumerable<uSyncAction> ReportHandler(string handler, uSyncImportOptions options)
        {
            var handlerPair = handlerFactory.GetValidHandler(handler, new SyncHandlerOptions
            {
                Set = options.HandlerSet,
                Action = HandlerActions.Report
            });

            if (handlerPair == null) return Enumerable.Empty<uSyncAction>();
            var folder = GetHandlerFolder(options.RootFolder, handlerPair.Handler);

            var orderedNodes = LoadOrderedNodes(folder);
            var total = orderedNodes.Count;
            var index = 0;
            var actions = new List<uSyncAction>();

            var itemHandler = GetItemHandlerOrDefault(handlerPair.Handler);

            foreach (var item in orderedNodes)
            {
                options.Callbacks?.Update.Invoke(item.Node.GetAlias(), index++, total);

                if (itemHandler != default)
                {
                    actions.AddRange(itemHandler.ReportElement(item.Node, item.FileName, handlerPair.Settings, options));
                }
                else
                {
                    actions.AddRange(handlerPair.Handler.ReportElement(item.Node));
                }
            }

            return actions;
        }

        public IEnumerable<uSyncAction> ImportHandler(string handlerAlias, uSyncImportOptions options)
        {

            lock (_importLock)
            {
                using (var pause = new uSyncImportPause())
                {
                    var handlerPair = handlerFactory.GetValidHandler(handlerAlias, new SyncHandlerOptions
                    {
                        Set = options.HandlerSet,
                        Action = HandlerActions.Import
                    });

                    if (handlerPair == null) return Enumerable.Empty<uSyncAction>();
                    var folder = GetHandlerFolder(options.RootFolder, handlerPair.Handler);

                    var orderedNodes = LoadOrderedNodes(folder);
                    var total = orderedNodes.Count;
                    var index = 0;
                    var actions = new List<uSyncAction>();

                    ISyncItemHandler itemHandler = GetItemHandlerOrDefault(handlerPair.Handler);

                    foreach (var item in orderedNodes)
                    {
                        options.Callbacks?.Update.Invoke(item.Node.GetAlias(), index++, total);

                        if (itemHandler != default)
                        {
                            actions.AddRange(itemHandler.ImportElement(item.Node, item.FileName, handlerPair.Settings, options));
                        }
                        else
                        {
                            actions.AddRange(handlerPair.Handler.ImportElement(item.Node, options.Flags.HasFlag(SerializerFlags.Force)));
                        }
                    }

                    foreach (var action in actions.Where(x => x.Success))
                    {
                        if (action.Change != Core.ChangeType.Clean && action.Item != null)
                        {
                            itemHandler.ImportSecondPass(action, handlerPair.Settings, options);
                        }
                    }

                    return actions;
                }
            }
        }

        public IEnumerable<uSyncAction> PerformPostImport(string rootFolder, string handlerSet, IEnumerable<uSyncAction> actions)
        {
            lock (_importLock)
            {
                using (var pause = new uSyncImportPause())
                {
                    var handlers = handlerFactory.GetValidHandlers(new SyncHandlerOptions { Set = handlerSet, Action = HandlerActions.Import });
                    return PerformPostImport(rootFolder, handlers, actions);
                }
            }
        }

        public IEnumerable<uSyncAction> ExportHandler(string handler, uSyncImportOptions options)
        {
            var handlerPair = handlerFactory.GetValidHandler(handler, new SyncHandlerOptions
            {
                Set = options.HandlerSet,
                Action = HandlerActions.Export
            });

            if (handlerPair == null) return Enumerable.Empty<uSyncAction>();
            var folder = GetHandlerFolder(options.RootFolder, handlerPair.Handler);

            return handlerPair.Handler.ExportAll(folder, handlerPair.Settings, options.Callbacks?.Update);
        }

        public void StartBulkProcess(HandlerActions action) 
        {
            switch (action)
            {
                case HandlerActions.Export:
                    WriteVersionFile(settings.RootFolder);
                    fireBulkStarting(ExportStarting);
                    break;
                case HandlerActions.Import:
                    fireBulkStarting(ImportStarting);
                    break;
                case HandlerActions.Report:
                    fireBulkStarting(ReportStarting);
                    break;
            }
        }

        public void FinishBulkProcess(HandlerActions action, IEnumerable<uSyncAction> actions) 
        {
            switch (action)
            {
                case HandlerActions.Export:
                    fireBulkComplete(ExportComplete, actions);
                    break;
                case HandlerActions.Import:
                    fireBulkComplete(ImportComplete, actions);
                    break;
                case HandlerActions.Report:
                    fireBulkComplete(ReportComplete, actions);
                    break;
            }
        }

        private ISyncItemHandler GetItemHandlerOrDefault(ISyncExtendedHandler handler)
        {
            if (handler is ISyncItemHandler) return handler as ISyncItemHandler;
            return default;
        }

        private string GetHandlerFolder(string rootFolder, ISyncHandler handler)
            => Path.Combine(rootFolder, handler.DefaultFolder);

    }
}
