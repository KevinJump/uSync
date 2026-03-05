namespace uSync.Core.Persistance;

public interface ISyncDataService<TModel, TKey> where TModel : class, ISyncDataEntity<TKey>
{
    Task CreateAsync(TModel item);
    Task DeleteAsync(TModel item);
    Task<bool> ExistsAsync(TKey key);
    Task<IEnumerable<TModel>> GetAllAsync(params TKey[] keys);
    Task<TModel?> GetAsync(TKey key);
    Task SaveAsync(TModel item);
    Task UpdateAsync(TModel item);
}