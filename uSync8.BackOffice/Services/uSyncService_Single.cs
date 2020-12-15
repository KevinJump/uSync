﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Owin.Security.DataHandler.Encoder;

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
        public IEnumerable<uSyncAction> ReportPartial(string folder, uSyncPagedImportOptions options, out int total)
        {
            var orderedNodes = LoadOrderedNodes(folder);
            total = orderedNodes.Count;

            var actions = new List<uSyncAction>();
            var lastType = string.Empty;

            SyncHandlerOptions syncHandlerOptions = new SyncHandlerOptions(options.HandlerSet);
            ExtendedHandlerConfigPair handlerPair = null;

            var index = options.PageNumber * options.PageSize;

            foreach (var item in orderedNodes.Skip(options.PageNumber * options.PageSize).Take(options.PageSize))
            {
                var itemType = item.Node.GetItemType();
                if (!itemType.InvariantEquals(lastType))
                {
                    lastType = itemType;
                    handlerPair = handlerFactory.GetValidHandlerByTypeName(itemType, syncHandlerOptions);
                }

                options.Callbacks?.Update.Invoke(item.Node.GetAlias(),
                    CalculateProgress(index, total, options.ProgressMin, options.ProgressMax), 100);

                if (handlerPair != null)
                {
                    if (handlerPair.Handler is ISyncItemHandler itemHandler)
                    {
                        actions.AddRange(itemHandler.ReportElement(item.Node, item.FileName, handlerPair.Settings, options));
                    }
                    else
                    {
                        actions.AddRange(handlerPair.Handler.ReportElement(item.Node));
                    }
                }

                index++;
            }

            return actions;
        }

        public IEnumerable<uSyncAction> ImportPartial(string folder, uSyncPagedImportOptions options, out int total)
        {
            lock (_importLock)
            {
                using (var pause = new uSyncImportPause())
                {
                    var orderedNodes = LoadOrderedNodes(folder);

                    total = orderedNodes.Count;

                    var actions = new List<uSyncAction>();
                    var lastType = string.Empty;

                    var range = options.ProgressMax - options.ProgressMin;

                    SyncHandlerOptions syncHandlerOptions = new SyncHandlerOptions(options.HandlerSet);
                    ExtendedHandlerConfigPair handlerPair = null;

                    var index = options.PageNumber * options.PageSize;

                    foreach (var item in orderedNodes.Skip(options.PageNumber * options.PageSize).Take(options.PageSize))
                    {
                        var itemType = item.Node.GetItemType();
                        if (!itemType.InvariantEquals(lastType))
                        {
                            lastType = itemType;
                            handlerPair = handlerFactory.GetValidHandlerByTypeName(itemType, syncHandlerOptions);
                        }

                        options.Callbacks?.Update?.Invoke(item.Node.GetAlias(), 
                            CalculateProgress(index, total, options.ProgressMin, options.ProgressMax), 100) ;

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

                        index++;
                    }

                    return actions;
                }
            }
        }

        public IEnumerable<uSyncAction> ImportPartialSecondPass(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options)
        {
            lock (_importLock)
            {
                using (var pause = new uSyncImportPause())
                {
                    SyncHandlerOptions syncHandlerOptions = new SyncHandlerOptions(options.HandlerSet);
                    var secondPassActions = new List<uSyncAction>();

                    var total = actions.Count();

                    var lastType = string.Empty;
                    ExtendedHandlerConfigPair handlerPair = null;

                    var index = options.PageNumber * options.PageSize;

                    foreach (var action in actions.Skip(options.PageNumber*options.PageSize).Take(options.PageSize))
                    {
                        if (!action.HandlerAlias.InvariantEquals(lastType))
                        {
                            lastType = action.HandlerAlias;    
                            handlerPair = handlerFactory.GetValidHandler(action.HandlerAlias, syncHandlerOptions);
                        }

                        options.Callbacks?.Update?.Invoke($"Second Pass: {action.Name}",
                            CalculateProgress(index, total, options.ProgressMin, options.ProgressMax), 100);


                        if (handlerPair != null && handlerPair.Handler is ISyncItemHandler itemHandler)
                        {
                            secondPassActions.AddRange(itemHandler.ImportSecondPass(action, handlerPair.Settings, options));
                        }

                        index++;
                    }

                    return secondPassActions;
                }
            }
        }
        
        public IEnumerable<uSyncAction> ImportPartialPostImport(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options)
        {
            lock (_importLock)
            {
                using (var pause = new uSyncImportPause())
                {

                    SyncHandlerOptions syncHandlerOptions = new SyncHandlerOptions(options.HandlerSet);

                    var aliases = actions.Select(x => x.HandlerAlias).Distinct();

                    var folders = actions
                        .Where(x => x.RequiresPostProcessing)
                        .Select(x => new { alias = x.HandlerAlias, folder = Path.GetDirectoryName(x.FileName), actions = x })
                        .DistinctBy(x => x.folder)
                        .GroupBy(x => x.alias)
                        .ToList();

                    var results = new List<uSyncAction>();

                    var index = 0;

                    foreach (var actionItem in folders.SelectMany(actionGroup => actionGroup))
                    {
                        var handlerPair = handlerFactory.GetValidHandler(actionItem.alias, syncHandlerOptions);
                        if (handlerPair.Handler is ISyncPostImportHandler postImportHandler)
                        {
                            options.Callbacks?.Update?.Invoke(actionItem.alias, index, folders.Count);

                            var handlerActions = actions.Where(x => x.HandlerAlias.InvariantEquals(handlerPair.Handler.Alias));
                            results.AddRange(postImportHandler.ProcessPostImport(actionItem.folder, handlerActions, handlerPair.Settings));
                        }
                        index++;
                    }

                    return results;
                }
            }
        }

        /// <summary>
        ///  Load the xml in a folder in level order so we process the higher level items first.
        /// </summary>
        private IList<OrderedNodeInfo> LoadOrderedNodes(string folder)
        {
            var files = syncFileService.GetFiles(folder, "*.config");

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


        /// <summary>
        ///  calculate the percentage progress we are making between a range. 
        /// </summary>
        /// <remarks>
        ///  for partial imports this allows the calling progress to smooth out the progress bar.
        /// </remarks>
        private int CalculateProgress(int value, int total, int min, int max)
            => (int)(min + (((float)value / total) * (max-min)));
                        
    }
}
