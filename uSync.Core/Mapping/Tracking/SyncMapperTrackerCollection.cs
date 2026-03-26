using Umbraco.Cms.Core.Composing;

namespace uSync.Core.Mapping.Tracking;

public class SyncMapperTrackerCollection
    : BuilderCollectionBase<ISyncMapperTracker>
{
    public SyncMapperTrackerCollection(Func<IEnumerable<ISyncMapperTracker>> items)
        : base(items)
    { }

    public async Task<IEnumerable<ISyncMapper>> GetMappersAsync(string editorAlias)
    {
        if (this.Count == 0) return Enumerable.Empty<ISyncMapper>();

        var tasks = this.Select(x => x.GetTrackingMappers(editorAlias));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(x => x);
    }
}

public class SyncMapperTrackerCollectionBuilder
    : LazyCollectionBuilderBase<SyncMapperTrackerCollectionBuilder, SyncMapperTrackerCollection, ISyncMapperTracker>
{
    protected override SyncMapperTrackerCollectionBuilder This => this;
}
