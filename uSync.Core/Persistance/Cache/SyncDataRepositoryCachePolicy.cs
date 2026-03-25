using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

using IScope = Umbraco.Cms.Infrastructure.Scoping.IScope;

namespace uSync.Core.Persistance.Cache;

internal class SyncDataRepositoryCachePolicy<TModel, TKey> 
    : ISyncDataRepositoryCachePolicy<TModel, TKey> where TModel : class, ISyncDataEntity<TKey>
{
    private readonly IAppPolicyCache _globalCache;
    private readonly IScopeAccessor _scopeAccessor;
    private readonly IRepositoryCacheVersionService _repositoryCacheVersionService;
    private readonly ICacheSyncService _cacheSyncService;

    public SyncDataRepositoryCachePolicy(
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


    private string _modelCacheKey => $"{nameof(TModel)}";
    private static readonly TModel[] _emptyArray = [];

    public async Task<TModel?> GetAsync(TKey key, Func<TKey, Task<TModel?>> performGetAsync, CancellationToken cancellationToken = default)
    {
        await EnsureCacheIsSyncedAsync();
        var cacheKey = GetCacheKey(key);

        TModel? fromCache = Cache.GetCacheItem<TModel>(cacheKey);
        if (fromCache is not null) return fromCache;

        // Cache the null result to prevent repeated lookups for missing items
        if (Cache.GetCacheItem<string>(cacheKey) == Constants.Cache.NullRepresentationInCache)
            return null;

        TModel? model = await performGetAsync(key);

        InsertIntoCache(cacheKey, model);

        return model;
    }

    public async Task<bool> ExistsAsync(TKey key, Func<TKey, Task<bool>> performExistsAsync, CancellationToken cancellationToken = default)
    {
        await EnsureCacheIsSyncedAsync();
        var cacheKey = GetCacheKey(key);
        TModel? fromCache = Cache.GetCacheItem<TModel>(cacheKey);
        return fromCache is not null || await performExistsAsync(key);
    }

    public async Task CreateAsync(TModel model, Func<TModel, Task> persistNewAsync, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            await persistNewAsync(model);
            Cache.Insert(GetCacheKey(model), () => model, TimeSpan.FromMinutes(5), true);
        }
        catch
        {
            Cache.Clear(GetCacheKey(model));
            Cache.Clear(_modelCacheKey);
            throw;
        }

        await RegisterCacheChangeAsync();
    }

    public async Task UpdateAsync(TModel model, Func<TModel, Task> persistUpdateAsync, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        try
        {
            await persistUpdateAsync(model);
            Cache.Insert(GetCacheKey(model), () => model, TimeSpan.FromMinutes(5), true);
        }
        catch
        {
            Cache.Clear(GetCacheKey(model));
            Cache.Clear(_modelCacheKey);
            throw;
        }

        await RegisterCacheChangeAsync();
    }

    public async Task DeleteAsync(TModel model, Func<TModel, Task> persistDeleteAsync, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            await persistDeleteAsync(model);
        }
        finally
        {
            Cache.Clear(GetCacheKey(model));
            Cache.Clear(_modelCacheKey);
        }

        await RegisterCacheChangeAsync();
    }

    public virtual async Task<TModel[]> GetAllAsync(TKey[]? keys, Func<TKey[]?, Task<IEnumerable<TModel>>> performGetAllAsync, CancellationToken cancellationToken = default)
    {
        await EnsureCacheIsSyncedAsync();

        if (keys?.Length > 0)
        {
            var items = await keys.ToAsyncEnumerable().Select(async (x, ct) => await GetCached(x, ct))
                .Where(x => x != null)
                .ToArrayAsync();

            if (items != null && keys.Length.Equals(items.Length))
                return [.. items.WhereNotNull()];
        }
        else
        {
            // TODO - we could choose to do a count, (cached vs db) to verify ?
            var items = Cache.GetCacheItemsByKeySearch<TModel>(_modelCacheKey)
                .WhereNotNull().ToArray();

            if (items.Length > 0)
                return items;
            else
            {
                // when the cache is empty  we check for the special 'empty' one.
                TModel[]? empty = Cache.GetCacheItem<TModel[]>(_modelCacheKey);
                if (empty != null)
                    return empty;
            }
        }

        var models = (await performGetAllAsync(keys))
            .WhereNotNull()
            .ToArray();

        InsertIntoCache(models);

        return models ?? [];
    }

    private string GetCacheKey(TModel model) => GetCacheKey(model.Key);

    private string GetCacheKey(TKey key)
    {
        if (EqualityComparer<TKey>.Default.Equals(key, default))
            return string.Empty;

        if (typeof(TKey).IsValueType)
            return $"{_modelCacheKey}_{key}";

        return $"{_modelCacheKey}_{key?.ToString()?.ToUpperInvariant()}";
    }

    private async Task EnsureCacheIsSyncedAsync()
    {
        var synced = await _repositoryCacheVersionService.IsCacheSyncedAsync<TModel>();
        if (synced) return;

        _cacheSyncService.SyncInternal(CancellationToken.None);
    }


    private async Task RegisterCacheChangeAsync()
        => await _repositoryCacheVersionService.SetCacheUpdatedAsync<TModel>();

    private void InsertIntoCache(string cacheKey, TModel? model)
    {
        if (model is null)
        {
            Cache.Insert(cacheKey, () => Constants.Cache.NullRepresentationInCache, TimeSpan.FromMinutes(5), true);
        }
        else
        {
            Cache.Insert(cacheKey, () => model, TimeSpan.FromMinutes(5), true);
        }
    }

    private void InsertIntoCache(TModel[]? models)
    {
        if (models == null || models.Length == 0)
        {
            Cache.Insert(_modelCacheKey, () => _emptyArray, TimeSpan.FromMinutes(5), true);
            return;
        }

        foreach (var model in models)
        {
            InsertIntoCache(GetCacheKey(model), model);
        }
    }

    private async Task<TModel?> GetCached(TKey key, CancellationToken cancellationToken)
    {
        await EnsureCacheIsSyncedAsync();
        var cacheKey = GetCacheKey(key);
        return Cache.GetCacheItem<TModel>(cacheKey);
    }

    public void ClearAllAsync()
       => Cache.ClearByKey(_modelCacheKey);
}
