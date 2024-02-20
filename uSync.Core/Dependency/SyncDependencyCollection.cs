using Umbraco.Cms.Core.Composing;

namespace uSync.Core.Dependency;

public class SyncDependencyCollection
    : BuilderCollectionBase<ISyncDependencyItem>
{
    public SyncDependencyCollection(Func<IEnumerable<ISyncDependencyItem>> items)
        : base(items) { }

    public IEnumerable<ISyncDependencyChecker<TObject>> GetCheckers<TObject>()
    {
        return this.Where(x => x is ISyncDependencyChecker<TObject> checker)
            .Select(x => x as ISyncDependencyChecker<TObject>);
    }
}

public class SyncDependencyCollectionBuilder
    : WeightedCollectionBuilderBase<SyncDependencyCollectionBuilder, SyncDependencyCollection, ISyncDependencyItem>
{
    protected override SyncDependencyCollectionBuilder This => this;
}
