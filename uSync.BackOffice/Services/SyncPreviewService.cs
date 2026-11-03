using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Scoping;

using uSync.BackOffice.Models;
using uSync.BackOffice.Preview;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice.Services;

/// <inheritdoc/>
internal class SyncPreviewService : ISyncPreviewService
{
    private readonly ISyncHandlerFactory _handlerFactory;
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly SyncPreviewMapperCollection _mappers;
    private readonly ILogger<SyncPreviewService> _logger;

    public SyncPreviewService(
        ISyncHandlerFactory handlerFactory,
        ICoreScopeProvider scopeProvider,
        SyncPreviewMapperCollection mappers,
        ILogger<SyncPreviewService> logger)
    {
        _handlerFactory = handlerFactory;
        _scopeProvider = scopeProvider;
        _mappers = mappers;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SyncPreviewResult?> GetPreviewAsync(string handlerAlias, Guid key)
    {
        var handlerConfig = _handlerFactory.GetValidHandler(handlerAlias);
        if (handlerConfig is null) return default;

        var node = await handlerConfig.Handler.TryFindItemNodeAsync(key);
        if (node is null) return default;

        // the scope is never completed, so anything the dry run writes is rolled back
        // and no notifications (or cache refreshes) are published.
        using var scope = _scopeProvider.CreateCoreScope(autoComplete: false);
        using var _ = scope.Notifications.Suppress();

        var resolver = new PendingItemResolver(this);
        var options = new SyncPreviewOptions { PendingItemResolver = resolver.ResolveAsync };

        var preview = await handlerConfig.Handler.GetPreviewAsync(node, handlerConfig.Settings, options);
        if (preview is null)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Handler {handler} doesn't support previews", handlerAlias);
            return default;
        }

        preview.PendingItems = resolver.Items;

        var result = new SyncPreviewResult { Preview = preview };

        var mapper = _mappers.GetMapper(preview);
        if (mapper is null)
        {
            // the backoffice shows just the changes list for items it can't preview.
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("No preview mapper for {handler} {itemType}", handlerAlias, preview.ItemType);
            return result;
        }

        result.EntityType = mapper.EntityType;
        result.Model = await mapper.MapAsync(preview);
        return result;
    }

    private static async Task<object?> BuildIncomingItemAsync(HandlerConfigPair handlerConfig, Guid key, SyncPreviewOptions? options)
    {
        var node = await handlerConfig.Handler.TryFindItemNodeAsync(key);
        if (node is null) return null;

        var preview = await handlerConfig.Handler.GetPreviewAsync(node, handlerConfig.Settings, options);
        return preview?.Incoming;
    }

    /// <summary>
    ///  finds items (by type and key) in the uSync folders for a single preview, building
    ///  each one with a dry run of its handler, and remembering what it has already built.
    /// </summary>
    private class PendingItemResolver
    {
        private readonly SyncPreviewService _service;
        private readonly Dictionary<(Type, Guid), object?> _items = [];

        public PendingItemResolver(SyncPreviewService service) => _service = service;

        /// <summary>
        ///  the items found and built so far.
        /// </summary>
        public IReadOnlyList<object> Items => [.. _items.Values.OfType<object>()];

        public async Task<object?> ResolveAsync(Type itemType, Guid key)
        {
            if (_items.TryGetValue((itemType, key), out var item)) return item;

            // mark it first, so an item that (somehow) depends on itself doesn't loop.
            _items[(itemType, key)] = null;

            var handlerConfig = _service._handlerFactory.GetValidHandlers()
                .FirstOrDefault(x => x.Handler.ItemType == itemType.Name);
            if (handlerConfig is null) return null;

            item = await BuildIncomingItemAsync(handlerConfig, key, new SyncPreviewOptions { PendingItemResolver = ResolveAsync });

            if (item is not null && _service._logger.IsEnabled(LogLevel.Debug))
                _service._logger.LogDebug("Preview using {type} {key} from the uSync files", itemType.Name, key);

            _items[(itemType, key)] = item;
            return item;
        }
    }
}
