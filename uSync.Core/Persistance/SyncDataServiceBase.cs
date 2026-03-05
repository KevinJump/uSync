using Umbraco.Cms.Core.Scoping;

namespace uSync.Core.Persistance;

internal class SyncDataServiceBase<TModel, TKey> : ISyncDataService<TModel, TKey> 
    where TModel : class, ISyncDataEntity<TKey>
{
    protected readonly ISyncDataRespository<TModel, TKey> Repository;
    protected readonly ICoreScopeProvider ScopeProvider;

    public SyncDataServiceBase(
        ISyncDataRespository<TModel, TKey> repository,
        ICoreScopeProvider scopeProvider)
    {
        Repository = repository;
        ScopeProvider = scopeProvider;
    }

    public virtual async Task CreateAsync(TModel item)
    {
        using var scope = ScopeProvider.CreateCoreScope(autoComplete: true);
        await Repository.CreateAsync(item);
        scope.Complete();
    }

    public virtual async Task UpdateAsync(TModel item)
    {
        using var scope = ScopeProvider.CreateCoreScope(autoComplete: true);
        await Repository.UpdateAsync(item);
        scope.Complete();
    }

    public virtual async Task SaveAsync(TModel item)
    {
        var existing = await GetAsync(item.Key);
        if (existing != null) 
        { 
            item.Id = existing.Id;
            await UpdateAsync(item);
        }
        else
        {
            await CreateAsync(item);
        }
    }

    public virtual async Task DeleteAsync(TModel item)
    {
        using var scope = ScopeProvider.CreateCoreScope(autoComplete: true);
        await Repository.DeleteAsync(item);
        scope.Complete();
    }

    public virtual async Task<bool> ExistsAsync(TKey key)
    {
        using var scope = ScopeProvider.CreateCoreScope(autoComplete: true);
        return await Repository.ExistsAsync(key);
    }

    public virtual async Task<TModel?> GetAsync(TKey key)
    {
        using var scope = ScopeProvider.CreateCoreScope(autoComplete: true);
        return await Repository.GetAsync(key);
    }

    public virtual async Task<IEnumerable<TModel>> GetAllAsync(params TKey[] keys)
    {
        using var scope = ScopeProvider.CreateCoreScope(autoComplete: true);
        return await Repository.GetAllAsync(keys);
    }
}
