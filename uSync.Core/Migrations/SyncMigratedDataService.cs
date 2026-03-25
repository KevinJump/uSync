using Umbraco.Cms.Core.Scoping;

using uSync.Core.Persistance;

namespace uSync.Core.Migrations;

internal class SyncMigratedDataService : SyncDataServiceBase<SyncMigratedData, string>,
    ISyncMigratedDataService
{
    private readonly ISyncMigratedDataRepository _migratedRepository;

    public SyncMigratedDataService(
        ISyncMigratedDataRepository migratedRepository,
        ICoreScopeProvider scopeProvider) : base(migratedRepository, scopeProvider)
    {
        _migratedRepository = migratedRepository;
    }

    /// <summary>
    ///  tells us if this property has had it's id migrated, 
    /// </summary>
    public async Task<string> GetOriginalKeyAsync(string key)
    {
        var item = await GetAsync(key);
        return item?.Orginal ?? key;
    }

    public async Task AddRename(string newKey, string oldKey, string? additionalData)
    {
        var item = new SyncMigratedData
        {
            Key = newKey,
            Orginal = oldKey,
            AdditionalData = additionalData
        };
        await SaveAsync(item);
    }

    public async Task DeleteAllAsync()
    {
        using(var scope = ScopeProvider.CreateCoreScope(autoComplete: true))
        {
           await _migratedRepository.DeleteAllAsync();
        }
    }
}
