using NPoco;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Persistence.SqlSyntax;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace uSync.Core.Persistance;

internal abstract class SyncDataRespositoryBase<TModel, Key> : ISyncDataRespository<TModel, Key>
    where TModel : class, ISyncDataEntity<Key>
{
    // protected readonly ISyncDataRepositoryCachePolicy<TModel, Key> _cachePolicy;
    protected readonly ISyncDataFullSetCachePolicy<TModel, Key> _cachePolicy;
    protected readonly IScopeAccessor _scopeAccessor;
    protected readonly AppCaches _appCaches;

    protected readonly string _tableName;

    public SyncDataRespositoryBase(
        IScopeAccessor scopeAccessor,
        AppCaches appCaches,
        ISyncDataFullSetCachePolicy<TModel, Key> cachePolicy,
        string tableName)
    {
        _scopeAccessor = scopeAccessor;
        _appCaches = appCaches;
        _cachePolicy = cachePolicy;

        _tableName = tableName;
    }

    protected IScope AmbientScope
    {
        get
        {

            {
                var scope = _scopeAccessor.AmbientScope
                    ?? throw new InvalidOperationException("No ambient scope found");
                return scope;
            }
        }
    }

    protected IUmbracoDatabase Database => AmbientScope.Database;
    protected ISqlContext SqlContext => AmbientScope.SqlContext;
    protected ISqlSyntaxProvider SqlSyntax => SqlContext.SqlSyntax;
    protected Sql<ISqlContext> Sql() => SqlContext.Sql();
    protected Sql<ISqlContext> Sql(string sql, params object[] args)
        => SqlContext.Sql(sql, args);

    protected virtual Sql<ISqlContext> GetBaseQuery(bool isCount)
        => isCount
            ? Sql().SelectCount().From<TModel>()
            : Sql().SelectAll().From<TModel>();

    protected virtual string GetBaseWhereClause()
        => $"{SqlSyntax.GetQuotedColumnName("Key")} = @Key";

    protected virtual IEnumerable<string> GetDeleteClauses()
        => [$"DELETE FROM {_tableName} WHERE {SqlSyntax.GetQuotedColumnName("Key")} = @Key"];

    public virtual async Task CreateAsync(TModel item)
        => await _cachePolicy.CreateAsync(item, PersistNewItemAsync);

    public virtual async Task UpdateAsync(TModel item)
        => await _cachePolicy.UpdateAsync(item, PersistUpdatedItemAsync);

    public virtual async Task DeleteAsync(TModel item)
        => await _cachePolicy.DeleteAsync(item, PersistDeletedItemAsync);

    public virtual async Task<bool> ExistsAsync(Key key)
        => await _cachePolicy.ExistsAsync(key, PerformGetAllAsync);

    public virtual async Task<TModel?> GetAsync(Key key)
        => await _cachePolicy.GetAsync(key, PerformGetAllAsync);

    public virtual async Task<IEnumerable<TModel>> GetAllAsync(params Key[] keys)
        => await _cachePolicy.GetAllAsync(keys, PerformGetAllAsync);

    private async Task PersistNewItemAsync(TModel model)
    {
        if (await ExistsAsync(model.Key))
            throw new InvalidOperationException($"An item with the id {model.Key} already exists.");

        using (var transaction = Database.GetTransaction())
        {
            await Database.InsertAsync(model);
            transaction.Complete();
        }
    }

    private async Task PersistUpdatedItemAsync(TModel model)
    {
        if (await ExistsAsync(model.Key) == false)
            throw new InvalidOperationException($"An item with the key {model.Key} does not exist.");

        using (var transaction = Database.GetTransaction())
        {
            await Database.UpdateAsync(model);
            transaction.Complete();
        }
    }

    private async Task PersistDeletedItemAsync(TModel model)
    {
        var deletes = GetDeleteClauses();
        foreach (var delete in deletes)
        {
            await Database.ExecuteAsync(delete, new { model.Key });
        }
    }

    private async Task<IEnumerable<TModel>> PerformGetAllAsync()
    {
        var sql = GetBaseQuery(false);
        return await Database.FetchAsync<TModel>(sql);
    }

}