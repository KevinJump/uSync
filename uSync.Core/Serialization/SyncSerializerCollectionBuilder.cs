using Umbraco.Cms.Core.Composing;
using Umbraco.Extensions;

namespace uSync.Core.Serialization;

public class SyncSerializerCollectionBuilder
    : WeightedCollectionBuilderBase<SyncSerializerCollectionBuilder, SyncSerializerCollection, ISyncSerializerBase>
{
    protected override SyncSerializerCollectionBuilder This => this;
}


public class SyncSerializerCollection : BuilderCollectionBase<ISyncSerializerBase>
{
    public SyncSerializerCollection(Func<IEnumerable<ISyncSerializerBase>> items)
        : base(items)
    { }

    public IEnumerable<ISyncSerializer<TObject>> GetSerializers<TObject>()
        => this.Where(x => x is ISyncSerializer<TObject> tracker && x is not null)
             .Select(x => x as ISyncSerializer<TObject>).WhereNotNull();

    public ISyncSerializer<TObject>? GetSerializer<TObject>()
        => this.Where(x => x is ISyncSerializer<TObject> tracker)
            .Select(x => x as ISyncSerializer<TObject>)
            .FirstOrDefault();

    public ISyncSerializer<TObject>? GetSerializer<TObject>(string name)
        => this.Where(x => x is ISyncSerializer<TObject> tracker && x.Name.InvariantEquals(name))
            .Select(x => x as ISyncSerializer<TObject>)
            .FirstOrDefault();

}
