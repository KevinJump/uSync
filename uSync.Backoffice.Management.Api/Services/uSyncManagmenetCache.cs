using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;

namespace uSync.Backoffice.Management.Api.Services;

/// <summary>
///  caches the current requests, so we can store all
///  the changes, and throw them back at the user at
///  the end of the process.
///  <para/>
///  progress/operation lookups are local-cache-first: the server actually
///  running a background operation always has the fastest, freshest answer in
///  its own <see cref="_progressCache"/>. Only when that misses (a status
///  request served by a different server, in a load-balanced backoffice) do we
///  fall back to the shared <c>umbracoLongRunningOperation</c> row - see
///  <see cref="SyncManagementSharedProgress"/>.
/// </summary>
internal class uSyncManagementCache : ISyncManagementCache
{
    private static readonly TimeSpan _maxAge = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<Guid, List<uSyncAction>> _actionCache = new();
    private readonly ConcurrentDictionary<Guid, SyncManagementProgress> _progressCache = new();
    private readonly ConcurrentDictionary<Guid, Guid> _operationRequestMap = new();

    private readonly ILongRunningOperationRepository _longRunningOperationRepository;
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly ILogger<uSyncManagementCache> _logger;

    public uSyncManagementCache(
        ILongRunningOperationRepository longRunningOperationRepository,
        ICoreScopeProvider scopeProvider,
        ILogger<uSyncManagementCache> logger)
    {
        _longRunningOperationRepository = longRunningOperationRepository;
        _scopeProvider = scopeProvider;
        _logger = logger;
    }

    public Guid GetNewCacheId()
        => Guid.NewGuid();

    public List<uSyncAction> GetCachedActions(Guid id)
        => _actionCache.TryGetValue(id, out var actions) ? actions : [];

    public bool IsValid(Guid id)
        => true;

    public void CacheItems(Guid id, IEnumerable<uSyncAction> actions, bool overwrite)
    {
        if (overwrite)
        {
            _actionCache.AddOrUpdate(id, [.. actions], (_, _) => [.. actions]);
            return;
        }

        var existing = GetCachedActions(id);
        existing.AddRange(actions);
        _actionCache.TryAdd(id, existing);
    }

    public void Clear(Guid id)
    {
        // only the raw action-merge cache is cleared here - it's called mid-process
        // (once the final steps have merged everything) and a fresh SaveProgress call
        // follows immediately after. The progress/operation-id records are left for
        // the status endpoint to serve until they age out via PruneExpired.
        _actionCache.TryRemove(id, out _);
    }

    public async Task SaveProgressAsync(SyncManagementProgress progress)
    {
        PruneExpired();

        progress.LastUpdated = DateTime.UtcNow;
        _progressCache.AddOrUpdate(progress.RequestId, progress, (_, _) => progress);

        // background runs only - normal-mode progress (no OperationId) never
        // needs to be visible from another server, so it never touches the DB.
        if (progress.OperationId is not Guid operationId) return;

        try
        {
            using ICoreScope scope = _scopeProvider.CreateCoreScope(autoComplete: true);

            var shared = new SyncManagementSharedProgress
            {
                RequestId = progress.RequestId,
                Action = progress.Action,
                Message = progress.Message,
                Complete = progress.Complete,
                Status = progress.Status,
                // interim writes stay small (summaries only) - the full action
                // list is only worth the write once the run has actually finished.
                Actions = progress.Complete ? [.. progress.Actions] : null,
            };

            await _longRunningOperationRepository.SetResultAsync(operationId, shared);
        }
        catch (Exception ex)
        {
            // the local cache write above already succeeded, so the run on this
            // server is unaffected - only cross-server visibility is degraded.
            _logger.LogWarning(ex, "Failed to save shared progress for uSync operation {OperationId}", operationId);
        }
    }

    public SyncManagementProgress? GetProgress(Guid requestId)
    {
        PruneExpired();
        return _progressCache.TryGetValue(requestId, out var progress) ? progress : null;
    }

    public async Task<SyncManagementProgress?> GetProgressForOperationAsync(Guid operationId)
    {
        var requestId = GetRequestIdForOperation(operationId);
        if (requestId is not null)
        {
            var local = GetProgress(requestId.Value);
            if (local is not null) return local;
        }

        try
        {
            using ICoreScope scope = _scopeProvider.CreateCoreScope(autoComplete: true);

            var operation = await _longRunningOperationRepository.GetAsync<SyncManagementSharedProgress>(operationId);
            var shared = operation?.Result;
            if (shared is null) return null;

            return new SyncManagementProgress
            {
                RequestId = shared.RequestId,
                OperationId = operationId,
                Action = shared.Action ?? string.Empty,
                Status = shared.Status ?? [],
                Actions = shared.Actions ?? [],
                Complete = shared.Complete,
                Message = shared.Message,
                LastUpdated = DateTime.UtcNow,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read shared progress for uSync operation {OperationId}", operationId);
            return null;
        }
    }

    public void RegisterOperation(Guid operationId, Guid requestId)
    {
        _operationRequestMap.AddOrUpdate(operationId, requestId, (_, _) => requestId);
    }

    public Guid? GetRequestIdForOperation(Guid operationId)
        => _operationRequestMap.TryGetValue(operationId, out var requestId) ? requestId : null;

    /// <summary>
    ///  prune anything we haven't touched in a while, so an abandoned run
    ///  (client never came back to collect the result) doesn't leak forever.
    /// </summary>
    private void PruneExpired()
    {
        var cutoff = DateTime.UtcNow - _maxAge;

        foreach (var entry in _progressCache)
        {
            if (entry.Value.LastUpdated >= cutoff) continue;

            _progressCache.TryRemove(entry.Key, out _);
            _actionCache.TryRemove(entry.Key, out _);

            foreach (var operation in _operationRequestMap.Where(x => x.Value == entry.Key).ToList())
            {
                _operationRequestMap.TryRemove(operation.Key, out _);
            }
        }
    }
}
