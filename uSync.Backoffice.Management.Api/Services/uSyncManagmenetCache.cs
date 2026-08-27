using System.Collections.Concurrent;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;

namespace uSync.Backoffice.Management.Api.Services;

/// <summary>
///  caches the current requests, so we can store all
///  the changes, and throw them back at the user at
///  the end of the process.
/// </summary>
internal class uSyncManagementCache : ISyncManagementCache
{
    private static readonly TimeSpan _maxAge = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<Guid, List<uSyncAction>> _actionCache = new();
    private readonly ConcurrentDictionary<Guid, SyncManagementProgress> _progressCache = new();
    private readonly ConcurrentDictionary<Guid, Guid> _operationRequestMap = new();

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

    public void SaveProgress(SyncManagementProgress progress)
    {
        PruneExpired();

        progress.LastUpdated = DateTime.UtcNow;
        _progressCache.AddOrUpdate(progress.RequestId, progress, (_, _) => progress);
    }

    public SyncManagementProgress? GetProgress(Guid requestId)
    {
        PruneExpired();
        return _progressCache.TryGetValue(requestId, out var progress) ? progress : null;
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
