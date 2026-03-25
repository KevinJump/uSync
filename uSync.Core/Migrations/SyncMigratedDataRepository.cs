using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

using uSync.Core.Migrations.Cache;
using uSync.Core.Persistance;

namespace uSync.Core.Migrations;

internal class SyncMigratedDataRepository
    : SyncDataRespositoryBase<SyncMigratedData, string>,
        ISyncMigratedDataRepository
{
    public SyncMigratedDataRepository(
        IScopeAccessor scopeAccessor,
        AppCaches appCaches,
        ISyncMigratedFullDataSetCachePolicy cachePolicy,
        ILogger<SyncDataRespositoryBase<SyncMigratedData, string>> logger)
        : base(scopeAccessor, logger, appCaches,
            cachePolicy, SyncMigrations.MigratedDataTableName)
    { }

    public async Task DeleteAllAsync()
    {
        var sql = Sql().Delete()
            .From<SyncMigratedData>();

        using(var transaction = Database.GetTransaction())
        {
            _ = await Database.ExecuteScalarAsync<int>(sql);
            transaction.Complete();
        }
    }
}
