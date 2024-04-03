using uSync.BackOffice;

namespace uSync.Backoffice.Management.Api.Services;
internal interface ISyncManagementCache
{
    void CacheItems(Guid id, IEnumerable<uSyncAction> actions, bool overwrite);
    List<uSyncAction> GetCachedActions(Guid id);
    Guid GetNewCacheId();
    bool IsValid(Guid id);

    void Clear(Guid id);
}