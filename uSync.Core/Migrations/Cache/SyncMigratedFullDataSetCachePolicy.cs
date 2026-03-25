using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Infrastructure.Scoping;

using uSync.Core.Persistance.Cache;

namespace uSync.Core.Migrations.Cache;

internal class SyncMigratedFullDataSetCachePolicy : SyncFullDataSetRepositoryCachePolicy<SyncMigratedData, string>
    , ISyncMigratedFullDataSetCachePolicy
{
    public SyncMigratedFullDataSetCachePolicy(
        IAppPolicyCache globalCache,
        IScopeAccessor scopeAccessor,
        IRepositoryCacheVersionService repositoryCacheVersionService,
        ICacheSyncService cacheSyncService)
        : base(globalCache, scopeAccessor, repositoryCacheVersionService, cacheSyncService)
    { }
}
