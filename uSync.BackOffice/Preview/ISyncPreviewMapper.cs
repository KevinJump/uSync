using System.Threading.Tasks;

using uSync.BackOffice.Models;

namespace uSync.BackOffice.Preview;

/// <summary>
///  turns a preview (the current and incoming items) into the model the backoffice
///  shows for that type of item.
/// </summary>
/// <remarks>
///  mappers are found by the type loader, so a package can add a preview for its own items
///  by adding a mapper, and registering a 'usync-item-preview' element for the
///  <see cref="EntityType"/> in the backoffice.
/// </remarks>
public interface ISyncPreviewMapper
{
    /// <summary>
    ///  the entity type the backoffice uses to pick the element that shows the preview (e.g. data-type).
    /// </summary>
    string EntityType { get; }

    /// <summary>
    ///  can this mapper map the preview.
    /// </summary>
    bool CanMap(SyncItemPreview preview);

    /// <summary>
    ///  build the model the backoffice element uses.
    /// </summary>
    /// <remarks>
    ///  this is called inside the preview scope, so it can read the database
    ///  (and see the items the dry run built), but nothing it writes is kept.
    /// </remarks>
    Task<object?> MapAsync(SyncItemPreview preview);
}
