namespace uSync.Core.Persistance;

public interface ISyncDataRespository<TModel, TKey> where TModel : class, ISyncDataEntity<TKey>
{
    Task CreateAsync(TModel item);
    Task DeleteAsync(TModel item);
    Task<bool> ExistsAsync(TKey key);
    Task<IEnumerable<TModel>> GetAllAsync(params TKey[] keys);
    Task<TModel?> GetAsync(TKey key);
    Task UpdateAsync(TModel item);
}