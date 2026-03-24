using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

using uSync.Core.Persistance;

using IScope = Umbraco.Cms.Infrastructure.Scoping.IScope;

namespace uSync.Core.Migrations;

/// <summary>
///  this is simliar to the SyncDataCachePolicy, except everything is cached in one key,
/// </summary>
/// <remarks>
///  caching all entites, works when it is unlikely they will change much duing the lookup
///  phase, and there are not a lot (e.g 100+s) of entrires, we can cache them, and then
///  all the lookups don't hit the database. 
/// </remarks>
internal class SyncMigrateFullDataSetCachePolicy<TModel, TKey> 
    : ISyncDataFullSetCachePolicy<TModel, TKey> 
    where TModel : class, ISyncDataEntity<TKey>
{
    private readonly IAppPolicyCache _globalCache;
    private readonly IScopeAccessor _scopeAccessor;
    private readonly IRepositoryCacheVersionService _repositoryCacheVersionService;
    private readonly ICacheSyncService _cacheSyncService;

    public SyncMigrateFullDataSetCachePolicy(
        IAppPolicyCache globalCache,
        IScopeAccessor scopeAccessor,
        IRepositoryCacheVersionService repositoryCacheVersionService,
        ICacheSyncService cacheSyncService)
    {
        _globalCache = globalCache;
        _scopeAccessor = scopeAccessor;
        _repositoryCacheVersionService = repositoryCacheVersionService;
        _cacheSyncService = cacheSyncService;
    }

    private IAppPolicyCache Cache
    {
        get
        {
            IScope? ambientScope = _scopeAccessor.AmbientScope;
            return (ambientScope?.RepositoryCacheMode) switch
            {
                RepositoryCacheMode.Default => _globalCache,
                RepositoryCacheMode.Scoped => ambientScope.IsolatedCaches.GetOrCreate<TModel>(),
                RepositoryCacheMode.None => NoAppCache.Instance,
                _ => throw new NotSupportedException($"RepositoryCacheMode {ambientScope?.RepositoryCacheMode} is not supported"),
            };
        }
    }
    private string _dataSetCacheKey => $"{typeof(TModel).FullName}_FullDataSet";

    public void ClearAllAsync()
        => Cache.ClearByKey(_dataSetCacheKey);

    public async Task CreateAsync(TModel model, Func<TModel, Task> persistNewAsync, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        try
        {
            await persistNewAsync(model);
        }
        finally
        {
            ClearAllAsync();
        }
    }

    public Task DeleteAsync(TModel model, Func<TModel, Task> persistDeleteAsync, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        try
        {
            return persistDeleteAsync(model);
        }
        finally
        {
            ClearAllAsync();
        }
    }

    public Task UpdateAsync(TModel model, Func<TModel, Task> persistUpdateAsync, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        try
        {
            return persistUpdateAsync(model);
        }
        finally
        {
            ClearAllAsync();
        }
    }

    public async Task<bool> ExistsAsync(TKey key, Func<Task<IEnumerable<TModel>>> performGetAllAsync, CancellationToken cancellationToken = default)
    {
        await EnsureCacheIsSyncedAsync();

        var all = await GetAllCached(performGetAllAsync, cancellationToken);
        return all.Any(x => x.Key?.Equals(key) is true);
    }

    public async Task<TModel[]> GetAllAsync(TKey[]? keys, Func<Task<IEnumerable<TModel>>> performGetAllAsync, CancellationToken cancellationToken = default)
    {
        await EnsureCacheIsSyncedAsync();

        var all = await GetAllCached(performGetAllAsync, cancellationToken);
        if (keys?.Length > 0)
        {
            return [.. all.Where(x => keys.Contains(x.Key))];
        }
        else
        {
            return all;
        }
    }

    public async Task<TModel?> GetAsync(TKey key, Func<Task<IEnumerable<TModel>>> performGetAllAsync, CancellationToken cancellationToken = default)
    {
        await EnsureCacheIsSyncedAsync();

        var all = await GetAllCached(performGetAllAsync, cancellationToken);
        return all.FirstOrDefault(x => x.Key?.Equals(key) is true);
    }

    SemaphoreSlim _semaphoreLock = new SemaphoreSlim(1);

    private async Task<TModel[]> GetAllCached(Func<Task<IEnumerable<TModel>>> performGetAllAsync, CancellationToken cancellationToken)
    {
        var all = Cache.GetCacheItem<TModel[]>(_dataSetCacheKey);
        if (all is not null) return all;

        try
        {
            await _semaphoreLock.WaitAsync(cancellationToken);
            
            // try in the lock, possible something else filled it in while we waited. 
            all = Cache.GetCacheItem<TModel[]>(_dataSetCacheKey);
            if (all is not null) return all;

            // go get the data from the database. 
            TModel[] entries = [.. (await performGetAllAsync())];
            await InsertCacheEntries(entries);
            return entries;
        }
        finally
        {
            _semaphoreLock.Release();
        }
    }

    private Task InsertCacheEntries(TModel[] entries)
    {
        Cache.Insert(_dataSetCacheKey, () => entries, TimeSpan.FromMinutes(10), true);
        return Task.CompletedTask;
    }


    private async Task EnsureCacheIsSyncedAsync()
    {
        var synced = await _repositoryCacheVersionService.IsCacheSyncedAsync<TModel>();
        if (synced) return;

        _cacheSyncService.SyncInternal(CancellationToken.None);
    }


}
