namespace uSync.Core.Persistance;

public interface ISyncDataEntity<TId>
{
    int Id { get; set; }

    TId Key { get; }

    bool HasIdentity => Id > 0;
}
