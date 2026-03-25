using uSync.Core.Migrations;

namespace uSync.Core.Persistance.Cache;

public interface ISyncMigratedDataCachePolicy 
    : ISyncDataRepositoryCachePolicy<SyncMigratedData, string>
{ }
