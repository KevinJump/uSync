using uSync.Core.Persistance;

namespace uSync.Core.Migrations;

public interface ISyncMigratedDataRepository : ISyncDataRespository<SyncMigratedData, string>
{
}
