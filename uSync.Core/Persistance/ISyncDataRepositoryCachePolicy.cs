namespace uSync.Core.Persistance;

public interface ISyncDataRepositoryCachePolicy<TModel, TKey> where TModel : class, ISyncDataEntity<TKey>
{
    void ClearAllAsync();
    Task CreateAsync(TModel model, Func<TModel, Task> persistNewAsync, CancellationToken cancellationToken = default);
    Task DeleteAsync(TModel model, Func<TModel, Task> persistDeleteAsync, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey key, Func<TKey, Task<bool>> performExistsAsync, CancellationToken cancellationToken = default);
    Task<TModel[]> GetAllAsync(TKey[]? keys, Func<TKey[]?, Task<IEnumerable<TModel>>> performGetAllAsync, CancellationToken cancellationToken = default);
    Task<TModel?> GetAsync(TKey key, Func<TKey, Task<TModel?>> performGetAsync, CancellationToken cancellationToken = default);
    Task UpdateAsync(TModel model, Func<TModel, Task> persistUpdateAsync, CancellationToken cancellationToken = default);
}