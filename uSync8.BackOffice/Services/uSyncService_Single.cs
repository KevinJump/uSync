using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;

using uSync8.BackOffice.SyncHandlers;

using uSync8.Core.Extensions;
using uSync8.Core.Serialization;

namespace uSync8.BackOffice
{

    // 
    // Implimentation of paged import methods.
    //
    public partial class uSyncService
    {
        public IEnumerable<uSyncAction> ImportPartial(string folder, uSyncImportOptions options)
        {
            lock (_importLock)
            {
                using (var pause = new uSyncImportPause())
                {
                    var orderedNodes = LoadOrderedNodes(folder);

                    var actions = new List<uSyncAction>();

                    var lastType = string.Empty;

                    SyncHandlerOptions syncHandlerOptions = new SyncHandlerOptions(options.HandlerSet);
                    ExtendedHandlerConfigPair handlerPair = null;

                    foreach (var item in orderedNodes.Skip(options.Page * options.PageSize).Take(options.PageSize))
                    {
                        if (!item.Node.Name.LocalName.InvariantEquals(lastType))
                        {
                            handlerPair = handlerFactory.GetValidHandlerByTypeName(item.Node.Name.LocalName, syncHandlerOptions);
                        }

                        if (handlerPair != null)
                        {
                            if (handlerPair.Handler is ISyncItemHandler itemHandler)
                            {
                                actions.AddRange(itemHandler.ImportElement(item.Node, item.FileName, handlerPair.Settings, options));
                            }
                            else
                            {
                                actions.AddRange(handlerPair.Handler.ImportElement(item.Node, options.Flags.HasFlag(SerializerFlags.Force)));
                            }
                        }
                    }

                    return actions;
                }
            }
        }

        public IEnumerable<uSyncAction> ImportPartialSecondPass(IEnumerable<uSyncAction> actions, uSyncImportOptions options)
        {
            lock (_importLock)
            {
                using (var pause = new uSyncImportPause())
                {
                    SyncHandlerOptions syncHandlerOptions = new SyncHandlerOptions(options.HandlerSet);
                    var secondPassActions = new List<uSyncAction>();

                    var lastType = string.Empty;
                    ExtendedHandlerConfigPair handlerPair = null;

                    foreach (var action in actions.Skip(options.Page * options.PageSize).Take(options.PageSize))
                    {
                        if (!action.HandlerAlias.InvariantEquals(lastType))
                        {
                            handlerPair = handlerFactory.GetValidHandler(action.HandlerAlias, syncHandlerOptions);
                        }

                        if (handlerPair != null && handlerPair.Handler is ISyncItemHandler itemHandler)
                        {
                            secondPassActions.AddRange(itemHandler.ImportSecondPass(action, handlerPair.Settings, options));
                        }
                    }

                    return secondPassActions;
                }
            }
        }
        
        public IEnumerable<uSyncAction> ImportPartialPostImport(IEnumerable<uSyncAction> actions, uSyncImportOptions options)
        {
            lock (_importLock)
            {
                using (var pause = new uSyncImportPause())
                {

                    SyncHandlerOptions syncHandlerOptions = new SyncHandlerOptions(options.HandlerSet);

                    var aliases = actions.Select(x => x.HandlerAlias).Distinct();

                    var folders = actions
                        .Select(x => new { alias = x.HandlerAlias, folder = Path.GetDirectoryName(x.FileName), actions = x })
                        .DistinctBy(x => x.folder)
                        .GroupBy(x => x.alias);

                    var results = new List<uSyncAction>();

                    foreach (var actionGroup in folders)
                    {
                        foreach (var actionItem in actionGroup)
                        {
                            var handlerPair = handlerFactory.GetValidHandler(actionItem.alias, syncHandlerOptions);
                            if (handlerPair.Handler is ISyncPostImportHandler postImportHandler)
                            {
                                var handlerActions = actions.Where(x => x.HandlerAlias.InvariantEquals(handlerPair.Handler.Alias));
                                results.AddRange(postImportHandler.ProcessPostImport(actionItem.folder, handlerActions, handlerPair.Settings));
                            }
                        }
                    }

                    return results;
                }
            }
        }

        /// <summary>
        ///  Load the xml in a folder in level order so we process the higher level items first.
        /// </summary>
        private IEnumerable<OrderedNodeInfo> LoadOrderedNodes(string folder)
        {
            var files = syncFileService.GetFiles(folder, "*.conifg");

            var nodes = new List<OrderedNodeInfo>();

            foreach(var file in files)
            {
                nodes.Add(new OrderedNodeInfo(file, syncFileService.LoadXElement(file)));
            }

            return nodes
                .OrderBy(x => x.Node.GetLevel())
                .ToList();
        }

        private class OrderedNodeInfo
        {
            public OrderedNodeInfo(string filename, XElement node)
            {
                this.FileName = filename;
                this.Node = node;
            }

            public XElement Node { get; set; }
            public string FileName { get; set; }
        }
    }
}
