using System.Collections.Concurrent;

using uSync.BackOffice;

namespace uSync.Backoffice.Management.Api.Services;

/// <summary>
///  caches the current requests, so we can store all
///  the changes, and throw them back at the user at
///  the end of the process. 
/// </summary>
internal class uSyncManagementCache : ISyncManagementCache
{
    private readonly ConcurrentDictionary<Guid, List<uSyncAction>> _actionCache = new();

    public Guid GetNewCacheId()
        => Guid.NewGuid();

    public List<uSyncAction> GetCachedActions(Guid id)
    {
        if (_actionCache.TryGetValue(id, out var actions))
            return actions;

        return new List<uSyncAction>();
    }

    public bool IsValid(Guid id)
        => true;

    public void CacheItems(Guid id, IEnumerable<uSyncAction> actions, bool overwrite)
    {
        if (overwrite)
        {
            _actionCache.TryAdd(id, actions.ToList());
            return;
        }

        var existing = GetCachedActions(id);
        existing.AddRange(actions);
        _actionCache.TryAdd(id, existing);
    }

}
