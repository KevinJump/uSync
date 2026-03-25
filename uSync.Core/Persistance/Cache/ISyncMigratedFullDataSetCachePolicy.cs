using uSync.Core.Migrations;

namespace uSync.Core.Persistance.Cache;

public interface ISyncMigratedFullDataSetCachePolicy 
    : ISyncFullDataSetRepositoryCachePolicy<SyncMigratedData, string>
{
    
}