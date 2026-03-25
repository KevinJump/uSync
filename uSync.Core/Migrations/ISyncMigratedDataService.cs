using uSync.Core.Persistance;

namespace uSync.Core.Migrations;

public interface ISyncMigratedDataService : ISyncDataService<SyncMigratedData, string>
{
    Task AddRename(string newKey, string oldKey, string? additionalData);
    Task DeleteAllAsync();
    Task<string> GetOriginalKeyAsync(string key);
}