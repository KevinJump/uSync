using System;
using System.Collections.Generic;
using System.Text;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Infrastructure.Scoping;

using uSync.Core.Persistance;

namespace uSync.Core.Migrations;

internal class SyncMigratedDataCachePolicy : SyncDataRepositoryCachePolicy<SyncMigratedData, string>
        , ISyncMigratedDataCachePolicy
{
    public SyncMigratedDataCachePolicy(
        IAppPolicyCache globalCache,
        IScopeAccessor scopeAccessor,
        IRepositoryCacheVersionService repositoryCacheVersionService,
        ICacheSyncService cacheSyncService)
        : base(globalCache, scopeAccessor, repositoryCacheVersionService, cacheSyncService)
    { }
}

public interface ISyncMigratedDataCachePolicy 
    : ISyncDataRepositoryCachePolicy<SyncMigratedData, string>
{ }