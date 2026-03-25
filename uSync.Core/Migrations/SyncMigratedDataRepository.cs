using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Infrastructure.Scoping;

using uSync.Core.Persistance;
using uSync.Core.Persistance.Cache;

namespace uSync.Core.Migrations;

internal class SyncMigratedDataRepository
    : SyncDataRespositoryBase<SyncMigratedData, string>,
        ISyncMigratedDataRepository
{
    public SyncMigratedDataRepository(
        IScopeAccessor scopeAccessor,
        AppCaches appCaches,
        ISyncMigratedFullDataSetCachePolicy cachePolicy,
        ILogger<SyncMigratedDataRepository> logger)
        : base(scopeAccessor, logger, appCaches,
            cachePolicy, SyncMigrations.MigratedDataTableName)
    { }
}
