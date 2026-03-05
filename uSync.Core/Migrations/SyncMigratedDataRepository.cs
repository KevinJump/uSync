using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Infrastructure.Scoping;

using uSync.Core.Persistance;

namespace uSync.Core.Migrations;

internal class SyncMigratedDataRepository
    : SyncDataRespositoryBase<SyncMigratedData, string>,
        ISyncMigratedDataRepository
{
    public SyncMigratedDataRepository(
        IScopeAccessor scopeAccessor,
        AppCaches appCaches,
        ISyncMigratedDataCachePolicy cachePolicy)
        : base(scopeAccessor, appCaches, cachePolicy, 
            SyncMigrations.MigratedDataTableName)
    { }
}
