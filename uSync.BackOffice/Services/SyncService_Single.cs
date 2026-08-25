using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Extensions;

using uSync.BackOffice.Extensions;
using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice;


/// <summary>
/// Implementation of paged import methods.
/// </summary>
public partial class SyncService
{
    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> ReportPartialAsync(IList<OrderedNodeInfo> orderedNodes, uSyncPagedImportOptions options)
    {
        var total = orderedNodes.Count;

        var actions = new List<uSyncAction>();
        var lastType = string.Empty;

        var folder = Path.GetDirectoryName(orderedNodes.FirstOrDefault()?.FileName ?? options.Folders?.FirstOrDefault() ?? _uSyncConfig.GetWorkingFolder()) ?? string.Empty;

        SyncHandlerOptions syncHandlerOptions = HandlerOptionsFromPaged(options);

        HandlerConfigPair? handlerPair = null;

        var index = options.PageNumber * options.PageSize;

        foreach (var item in orderedNodes.Skip(options.PageNumber * options.PageSize).Take(options.PageSize))
        {
            var itemType = item.Node.GetItemType();
            if (!itemType.InvariantEquals(lastType))
            {
                lastType = itemType;
                handlerPair = _handlerFactory.GetValidHandlerByTypeName(itemType, syncHandlerOptions);

                if (handlerPair is null)
                    continue;

                List<Guid> keys = [.. orderedNodes.Select(x => x.Key)];
                await handlerPair.Handler.PreCacheFolderKeysAsync(folder, keys);
            }


            if (options.Callbacks is not null)
                await options.Callbacks.RaiseUpdateAsync(item.Node.GetAlias(),
                    CalculateProgress(index, total, options.ProgressMin, options.ProgressMax), 100);

            if (handlerPair != null)
            {
                actions.AddRange(await handlerPair.Handler.ReportElementAsync(item.Node, item.FileName, handlerPair.Settings, options));
            }

            index++;
        }

        return actions;
    }

    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> ImportPartialAsync(IList<OrderedNodeInfo> orderedNodes, uSyncPagedImportOptions options)
    {
        if (await _importSemaphoreLock.WaitAsync(_importLockWaitTimeSpan) is false)
            throw new TimeoutException($"Could not acquire import lock within the configured timespan of {_importLockWaitTimeSpan.TotalSeconds} seconds.");

        try
        {
            var total = orderedNodes.Count;

            using (var pause = _mutexService.ImportPause(options.PauseDuringImport))
            {
                var actions = new List<uSyncAction>();
                var lastType = string.Empty;

                var range = options.ProgressMax - options.ProgressMin;

                SyncHandlerOptions syncHandlerOptions = HandlerOptionsFromPaged(options);

                HandlerConfigPair? handlerPair = null;

                var index = options.PageNumber * options.PageSize;

                using var scope = _scopeProvider.CreateNotificationScope(
                    eventAggregator: _eventAggregator,
                    loggerFactory: _loggerFactory,
                    syncConfigService: _uSyncConfig,
                    syncEventService: _mutexService,
                    backgroundTaskQueue: _backgroundTaskQueue,
                    options.Callbacks?.Update);
                {
                    try
                    {
                        foreach (var item in orderedNodes.Skip(options.PageNumber * options.PageSize).Take(options.PageSize))
                        {
                            var node = item.Node ?? await _syncFileService.LoadXElementAsync(item.FileName);

                            var itemType = node.GetItemType();
                            if (!itemType.InvariantEquals(lastType))
                            {
                                lastType = itemType;
                                handlerPair = _handlerFactory.GetValidHandlerByTypeName(itemType, syncHandlerOptions);

                                // special case, blueprints looks like IContent items, except they are slightly different
                                // so we check for them specifically and get the handler for the entity rather than the object type.
                                if (node.IsContent() && node.IsBlueprint())
                                {
                                    lastType = UdiEntityType.DocumentBlueprint;
                                    handlerPair = _handlerFactory.GetValidHandlerByEntityType(UdiEntityType.DocumentBlueprint, syncHandlerOptions);
                                }
                            }

                            if (handlerPair == null)
                            {
                                _logger.LogWarning("No handler was found for {alias} item might not process correctly", itemType);
                                continue;
                            }

                            if (options.Callbacks is not null)
                                await options.Callbacks.RaiseUpdateAsync(node.GetAlias(),
                                    CalculateProgress(index, total, options.ProgressMin, options.ProgressMax), 100);

                            if (handlerPair != null)
                            {
                                actions.AddRange(await handlerPair.Handler.ImportElementAsync(node, item.FileName, handlerPair.Settings, options));
                            }

                            index++;
                        }
                    }
                    finally
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug("Imported {count} items", actions.Count);

                        scope?.Complete();
                    }

                }

                return actions;
            }
        }
        finally
        {
            _importSemaphoreLock.Release();
        }
    }

    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> ImportPartialSecondPassAsync(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options)
    {
        if (await _importSemaphoreLock.WaitAsync(_importLockWaitTimeSpan) is false)
            throw new TimeoutException($"Could not acquire import lock within the configured timespan of {_importLockWaitTimeSpan.TotalSeconds} seconds.");

        try
        {
            using (var pause = _mutexService.ImportPause(options.PauseDuringImport))
            {
                SyncHandlerOptions syncHandlerOptions = HandlerOptionsFromPaged(options);

                var secondPassActions = new List<uSyncAction>();

                var total = actions.Count();

                var lastType = string.Empty;
                HandlerConfigPair? handlerPair = null;

                var index = options.PageNumber * options.PageSize;

                using (var scope = _scopeProvider.CreateNotificationScope(
                    eventAggregator: _eventAggregator,
                    loggerFactory: _loggerFactory,
                    syncConfigService: _uSyncConfig,
                    syncEventService: _mutexService,
                    backgroundTaskQueue: _backgroundTaskQueue,
                    options.Callbacks?.Update))
                {
                    try
                    {
                        foreach (var action in actions.Skip(options.PageNumber * options.PageSize).Take(options.PageSize))
                        {
                            if (action.HandlerAlias is null) continue;

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

                            if (options.Callbacks is not null)
                                await options.Callbacks.RaiseUpdateAsync($"Second Pass: {action.Name}",
                                    CalculateProgress(index, total, options.ProgressMin, options.ProgressMax), 100);

                            secondPassActions.AddRange(await handlerPair.Handler.ImportSecondPassAsync(action, handlerPair.Settings, options));

                            index++;
                        }
                    }
                    finally
                    {
                        scope?.Complete();
                    }
                }

                return secondPassActions;
            }
        }
        finally
        {
            _importSemaphoreLock.Release();
        }
    }

    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> ImportPartialPostImportAsync(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options)
    {
        if (actions == null || !actions.Any()) return [];

        if (await _importSemaphoreLock.WaitAsync(_importLockWaitTimeSpan) is false)
            throw new TimeoutException($"Could not acquire import lock within the configured timespan of {_importLockWaitTimeSpan.TotalSeconds} seconds.");

        try
        {
            using (var pause = _mutexService.ImportPause(options.PauseDuringImport))
            {

                SyncHandlerOptions syncHandlerOptions = HandlerOptionsFromPaged(options);

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
                    if (actionItem.alias is null) continue;

                    var handlerPair = _handlerFactory.GetValidHandler(actionItem.alias, syncHandlerOptions);

                    if (handlerPair == null)
                    {
                        _logger.LogWarning("No handler was found for {alias} item might not process correctly", actionItem.alias);
                    }
                    else
                    {
                        if (handlerPair.Handler is ISyncPostImportHandler postImportHandler)
                        {
                            if (options.Callbacks is not null)
                                await options.Callbacks.RaiseUpdateAsync(actionItem.alias, index, folders.Count);

                            var handlerActions = actions.Where(x => x.HandlerAlias.InvariantEquals(handlerPair.Handler.Alias));
                            results.AddRange(await postImportHandler.ProcessPostImportAsync(handlerActions, handlerPair.Settings));
                        }
                    }

                    index++;
                }

                return results;
            }
        }
        finally
        {
            _importSemaphoreLock.Release();
        }
    }

    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> ImportPostCleanFilesAsync(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options)
    {
        if (actions == null) return [];

        if (await _importSemaphoreLock.WaitAsync(_importLockWaitTimeSpan) is false)
            throw new TimeoutException($"Could not acquire import lock within the configured timespan of {_importLockWaitTimeSpan.TotalSeconds} seconds.");

        try
        {
            using (var pause = _mutexService.ImportPause(options.PauseDuringImport))
            {
                SyncHandlerOptions syncHandlerOptions = new(options.HandlerSet, options.UserId);

                var cleans = actions
                    .Where(x => x.Change == ChangeType.Clean && !string.IsNullOrWhiteSpace(x.FileName))
                    .Select(x => new { alias = x.HandlerAlias, folder = Path.GetDirectoryName(x.FileName), actions = x })
                    .DistinctBy(x => x.folder)
                    .GroupBy(x => x.alias)
                    .ToList();

                var results = new List<uSyncAction>();

                var index = 0;

                foreach (var actionItem in cleans.SelectMany(actionGroup => actionGroup))
                {
                    if (actionItem.alias is null) continue;

                    var handlerPair = _handlerFactory.GetValidHandler(actionItem.alias, syncHandlerOptions);
                    if (handlerPair is null) continue;

                    if (handlerPair.Handler is ISyncCleanEntryHandler cleanEntryHandler)
                    {
                        if (options.Callbacks is not null)
                            await options.Callbacks.RaiseUpdateAsync(actionItem.alias, index, cleans.Count);

                        var handlerActions = actions.Where(x => x.HandlerAlias.InvariantEquals(handlerPair.Handler.Alias));
                        results.AddRange(await cleanEntryHandler.ProcessCleanActionsAsync(actionItem.folder, handlerActions, handlerPair.Settings));
                    }
                    index++;
                }

                return results;
            }
        }
        finally
        {
            _importSemaphoreLock.Release();
        }
    }

    private static SyncHandlerOptions HandlerOptionsFromPaged(uSyncPagedImportOptions options)
        => new(options.HandlerSet, options.UserId)
        {
            IncludeDisabled = options.IncludeDisabledHandlers
        };

    /// <inheritdoc/>>
    public async Task<IList<OrderedNodeInfo>> LoadOrderedNodesAsync(ISyncHandler handler, string[] handlerFolders)
        => [.. (await handler.FetchAllNodesAsync(handlerFolders))];

    /// <summary>
    ///  calculate the percentage progress we are making between a range. 
    /// </summary>
    /// <remarks>
    ///  for partial imports this allows the calling progress to smooth out the progress bar.
    /// </remarks>
    private static int CalculateProgress(int value, int total, int min, int max)
        => (int)(min + (((float)value / total) * (max - min)));


    /// <summary>
    ///  export a single item via the correct handler. 
    /// </summary>
    /// <remarks>
    ///  this method isn't used directly in the code, but is here as a helper .
    /// </remarks>
    public async Task<IEnumerable<uSyncAction>> ExportSingleItem(Udi udi, string[] folders, SyncHandlerOptions options)
    {
        var handler = _handlerFactory.GetValidHandlerByEntityType(udi.EntityType, options);
        if (handler == null) return [uSyncAction.Fail("Single", "Unknown", udi.EntityType, ChangeType.Fail, $"Could not find handler for entity type '{udi.EntityType}'", new KeyNotFoundException(udi.EntityType))];
        return await handler.Handler.ExportAsync(udi, folders, handler.Settings);
    }
}
