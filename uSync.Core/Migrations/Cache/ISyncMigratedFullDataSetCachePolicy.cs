using uSync.Core.Persistance.Cache;

namespace uSync.Core.Migrations.Cache;

public interface ISyncMigratedFullDataSetCachePolicy 
    : ISyncFullDataSetRepositoryCachePolicy<SyncMigratedData, string>
{
    
}