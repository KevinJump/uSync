using System.Threading.Tasks;

using uSync.BackOffice.Models;

namespace uSync.BackOffice.Preview;

/// <summary>
///  base for a preview mapper that maps one type of item.
/// </summary>
public abstract class SyncPreviewMapperBase<TItem> : ISyncPreviewMapper
    where TItem : class
{
    /// <inheritdoc/>
    public abstract string EntityType { get; }

    /// <inheritdoc/>
    public virtual bool CanMap(SyncItemPreview preview)
        => (preview.Current ?? preview.Incoming) is TItem;

    /// <inheritdoc/>
    public Task<object?> MapAsync(SyncItemPreview preview)
        => MapAsync(preview, preview.Current as TItem, preview.Incoming as TItem);

    /// <summary>
    ///  build the model from the current item (null when it isn't in the site yet)
    ///  and the incoming one (null when the import would delete it).
    /// </summary>
    protected abstract Task<object?> MapAsync(SyncItemPreview preview, TItem? current, TItem? incoming);
}
