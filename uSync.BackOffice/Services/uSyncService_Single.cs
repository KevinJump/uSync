using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Extensions;

using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice
{

    /// <summary>
    /// Implimentation of paged import methods.
    /// </summary>
    public partial class uSyncService
    {
        /// <summary>
        ///  Peform a paged report against a given folder 
        /// </summary>
        public IEnumerable<uSyncAction> ReportPartial(string folder, uSyncPagedImportOptions options, out int total)
        {
            var orderedNodes = LoadOrderedNodes(folder);
            total = orderedNodes.Count;

            var actions = new List<uSyncAction>();
            var lastType = string.Empty;

            SyncHandlerOptions syncHandlerOptions = HandlerOptionsFromPaged(options);

            HandlerConfigPair handlerPair = null;

            var index = options.PageNumber * options.PageSize;

            foreach (var item in orderedNodes.Skip(options.PageNumber * options.PageSize).Take(options.PageSize))
            {
                var itemType = item.Node.GetItemType();
                if (!itemType.InvariantEquals(lastType))
                {
                    lastType = itemType;
                    handlerPair = _handlerFactory.GetValidHandlerByTypeName(itemType, syncHandlerOptions);
                }

                if (handlerPair == null)
                {
                    _logger.LogWarning("No handler was found for {alias} item might not process correctly", itemType);
                    continue;
                }

                options.Callbacks?.Update.Invoke(item.Node.GetAlias(),
                    CalculateProgress(index, total, options.ProgressMin, options.ProgressMax), 100);

                if (handlerPair != null)
                {
                    actions.AddRange(handlerPair.Handler.ReportElement(item.Node, item.FileName, handlerPair.Settings, options));
                }

                index++;
            }

            return actions;
        }

        /// <summary>
        ///  Peform a paged Import against a given folder 
        /// </summary>
        public IEnumerable<uSyncAction> ImportPartial(string folder, uSyncPagedImportOptions options, out int total)
        {
            lock (_importLock)
            {
                using (var pause = _mutexService.ImportPause(options.PauseDuringImport))
                {
                    var orderedNodes = LoadOrderedNodes(folder);

                    total = orderedNodes.Count;

                    var actions = new List<uSyncAction>();
                    var lastType = string.Empty;

                    var range = options.ProgressMax - options.ProgressMin;

                    SyncHandlerOptions syncHandlerOptions = HandlerOptionsFromPaged(options);

                    HandlerConfigPair handlerPair = null;

                    var index = options.PageNumber * options.PageSize;

                    foreach (var item in orderedNodes.Skip(options.PageNumber * options.PageSize).Take(options.PageSize))
                    {
                        var itemType = item.Node.GetItemType();
                        if (!itemType.InvariantEquals(lastType))
                        {
                            lastType = itemType;
                            handlerPair = _handlerFactory.GetValidHandlerByTypeName(itemType, syncHandlerOptions);

                            // special case, blueprints looks like IContent items, except they are slightly diffrent
                            // so we check for them speicifically and get the handler for the enity rather than the object type.
                            if (item.Node.IsContent() && item.Node.IsBlueprint())
                            {
                                lastType = UdiEntityType.DocumentBlueprint;
                                handlerPair = _handlerFactory.GetValidHandlerByEntityType(UdiEntityType.DocumentBlueprint);
                            }
                        }

                        if (handlerPair == null)
                        {
                            _logger.LogWarning("No handler was found for {alias} item might not process correctly", itemType);
                            continue;
                        }

                        options.Callbacks?.Update?.Invoke(item.Node.GetAlias(),
                            CalculateProgress(index, total, options.ProgressMin, options.ProgressMax), 100);

                        if (handlerPair != null)
                        {
                            actions.AddRange(handlerPair.Handler.ImportElement(item.Node, item.FileName, handlerPair.Settings, options));
                        }

                        index++;
                    }

                    return actions;
                }
            }
        }

        /// <summary>
        ///  Peform a paged Import second pass against a given folder 
        /// </summary>
        public IEnumerable<uSyncAction> ImportPartialSecondPass(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options)
        {
            lock (_importLock)
            {
                using (var pause = _mutexService.ImportPause(options.PauseDuringImport))
                {
                    SyncHandlerOptions syncHandlerOptions = HandlerOptionsFromPaged(options);

                    var secondPassActions = new List<uSyncAction>();

                    var total = actions.Count();

                    var lastType = string.Empty;
                    HandlerConfigPair handlerPair = null;

                    var index = options.PageNumber * options.PageSize;

                    foreach (var action in actions.Skip(options.PageNumber * options.PageSize).Take(options.PageSize))
                    {
                        if (!action.HandlerAlias.InvariantEquals(lastType))
                        {
                            lastType = action.HandlerAlias;
                            handlerPair = _handlerFactory.GetValidHandler(action.HandlerAlias, syncHandlerOptions);
                        }

                        if (handlerPair == null)
                        {
                            _logger.LogWarning("No handler was found for {alias} item might not process correctly", action.HandlerAlias);
                            continue;
                        }

                        options.Callbacks?.Update?.Invoke($"Second Pass: {action.Name}",
                            CalculateProgress(index, total, options.ProgressMin, options.ProgressMax), 100);

                        secondPassActions.AddRange(handlerPair.Handler.ImportSecondPass(action, handlerPair.Settings, options));

                        index++;
                    }

                    return secondPassActions;
                }
            }
        }

        /// <summary>
        ///  Peform a paged Import post import against a given folder 
        /// </summary>
        public IEnumerable<uSyncAction> ImportPartialPostImport(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options)
        {
            if (actions == null || !actions.Any()) return Enumerable.Empty<uSyncAction>();

            lock (_importLock)
            {
                using (var pause = _mutexService.ImportPause(options.PauseDuringImport))
                {

                    SyncHandlerOptions syncHandlerOptions = HandlerOptionsFromPaged(options);

                    var aliases = actions.Select(x => x.HandlerAlias).Distinct();

                    var folders = actions
                        .Where(x => x.RequiresPostProcessing)
                        .Select(x => new { alias = x.HandlerAlias, folder = Path.GetDirectoryName(x.FileName), actions = x })
                        .SafeDistinctBy(x => x.folder)
                        .GroupBy(x => x.alias)
                        .ToList();

                    var results = new List<uSyncAction>();

                    var index = 0;

                    foreach (var actionItem in folders.SelectMany(actionGroup => actionGroup))
                    {
                        var handlerPair = _handlerFactory.GetValidHandler(actionItem.alias, syncHandlerOptions);

                        if (handlerPair == null)
                        {
                            _logger.LogWarning("No handler was found for {alias} item might not process correctly", actionItem.alias);
                        }
                        else
                        {
                            if (handlerPair.Handler is ISyncPostImportHandler postImportHandler)
                            {
                                options.Callbacks?.Update?.Invoke(actionItem.alias, index, folders.Count);

                                var handlerActions = actions.Where(x => x.HandlerAlias.InvariantEquals(handlerPair.Handler.Alias));
                                results.AddRange(postImportHandler.ProcessPostImport(actionItem.folder, handlerActions, handlerPair.Settings));
                            }
                        }

                        index++;
                    }

                    return results;
                }
            }
        }

        /// <summary>
        ///  Peform a paged Clean after import for a given folder 
        /// </summary>
        public IEnumerable<uSyncAction> ImportPostCleanFiles(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options)
        {
            if (actions == null) return Enumerable.Empty<uSyncAction>();

            lock (_importLock)
            {
                using (var pause = _mutexService.ImportPause(options.PauseDuringImport))
                {
                    SyncHandlerOptions syncHandlerOptions = new SyncHandlerOptions(
                        options.HandlerSet, options.UserId);

                    var cleans = actions
                        .Where(x => x.Change == ChangeType.Clean && !string.IsNullOrWhiteSpace(x.FileName))
                        .Select(x => new { alias = x.HandlerAlias, folder = Path.GetDirectoryName(x.FileName), actions = x })
                        .SafeDistinctBy(x => x.folder)
                        .GroupBy(x => x.alias)
                        .ToList();

                    var results = new List<uSyncAction>();

                    var index = 0;

                    foreach (var actionItem in cleans.SelectMany(actionGroup => actionGroup))
                    {
                        var handlerPair = _handlerFactory.GetValidHandler(actionItem.alias, syncHandlerOptions);
                        if (handlerPair.Handler is ISyncCleanEntryHandler cleanEntryHandler)
                        {
                            options.Callbacks?.Update?.Invoke(actionItem.alias, index, cleans.Count);

                            var handlerActions = actions.Where(x => x.HandlerAlias.InvariantEquals(handlerPair.Handler.Alias));
                            results.AddRange(cleanEntryHandler.ProcessCleanActions(actionItem.folder, handlerActions, handlerPair.Settings));
                        }
                        index++;
                    }

                    return results;
                }
            }
        }

        private SyncHandlerOptions HandlerOptionsFromPaged(uSyncPagedImportOptions options)
            => new SyncHandlerOptions(options.HandlerSet, options.UserId)
            {
                IncludeDisabled = options.IncludeDisabledHandlers
            };

        /// <summary>
        ///  Load the xml in a folder in level order so we process the higher level items first.
        /// </summary>
        private IList<OrderedNodeInfo> LoadOrderedNodes(string folder)
        {
            var files = _syncFileService.GetFiles(folder, $"*.{_uSyncConfig.Settings.DefaultExtension}", true);

            var nodes = new List<OrderedNodeInfo>();

            foreach (var file in files)
            {
                nodes.Add(new OrderedNodeInfo(file, _syncFileService.LoadXElement(file)));
            }

            return nodes
                .OrderBy(x => (x.Node.GetLevel() * 1000) + x.Node.GetItemSortOrder())
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
            => (int)(min + (((float)value / total) * (max - min)));

    }
}
