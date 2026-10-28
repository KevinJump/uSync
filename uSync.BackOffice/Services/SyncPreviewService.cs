using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Scoping;

using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Services;

/// <inheritdoc/>
internal class SyncPreviewService : ISyncPreviewService
{
    private readonly ISyncHandlerFactory _handlerFactory;
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly ILogger<SyncPreviewService> _logger;

    public SyncPreviewService(
        ISyncHandlerFactory handlerFactory,
        ICoreScopeProvider scopeProvider,
        ILogger<SyncPreviewService> logger)
    {
        _handlerFactory = handlerFactory;
        _scopeProvider = scopeProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TResult?> GetPreviewAsync<TResult>(string handlerAlias, Guid key, Func<SyncItemPreview, TResult> mapper)
    {
        var handlerConfig = _handlerFactory.GetValidHandler(handlerAlias);
        if (handlerConfig is null) return default;

        var node = await handlerConfig.Handler.TryFindItemNodeAsync(key);
        if (node is null) return default;

        // the scope is never completed, so anything the dry run writes is rolled back
        // and no notifications (or cache refreshes) are published.
        using var scope = _scopeProvider.CreateCoreScope(autoComplete: false);
        using var _ = scope.Notifications.Suppress();

        var preview = await handlerConfig.Handler.GetPreviewAsync(node, handlerConfig.Settings);
        if (preview is null)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Handler {handler} doesn't support previews", handlerAlias);
            return default;
        }

        return mapper(preview);
    }
}
