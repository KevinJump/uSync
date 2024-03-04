using Umbraco.Cms.Core.Composing;
using Umbraco.Extensions;

namespace uSync.Core.Dependency;

/// <summary>
///  collection of dependency checkers.
/// </summary>
public class SyncDependencyCollection
    : BuilderCollectionBase<ISyncDependencyItem>
{
    public SyncDependencyCollection(Func<IEnumerable<ISyncDependencyItem>> items)
        : base(items) { }

    public IEnumerable<ISyncDependencyChecker<TObject>> GetCheckers<TObject>()
    {
        return this.Where(x => x is ISyncDependencyChecker<TObject> checker)?
            .Select(x => x as ISyncDependencyChecker<TObject>).WhereNotNull() ?? [];
    }
}

/// <summary>
///  collection builder to build collection of dependency checkers. 
/// </summary>
public class SyncDependencyCollectionBuilder
    : WeightedCollectionBuilderBase<SyncDependencyCollectionBuilder, SyncDependencyCollection, ISyncDependencyItem>
{
    protected override SyncDependencyCollectionBuilder This => this;
}
