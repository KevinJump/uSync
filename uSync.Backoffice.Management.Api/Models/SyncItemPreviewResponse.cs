using uSync.Core.Models;

namespace uSync.Backoffice.Management.Api.Models;

/// <summary>
///  current and incoming versions of an item, in the shape the backoffice uses.
/// </summary>
public class SyncItemPreviewResponse
{
    public required Guid Key { get; set; }

    public required string Handler { get; set; }

    public required string ItemType { get; set; }

    /// <summary>
    ///  the backoffice entity type for the item (e.g. data-type), empty when there is no typed preview.
    /// </summary>
    /// <remarks>
    ///  the backoffice shows the 'usync-item-preview' element registered for this entity type.
    /// </remarks>
    public string EntityType { get; set; } = string.Empty;

    public bool IsDelete { get; set; }

    public bool Success { get; set; }

    public string? Message { get; set; }

    /// <summary>
    ///  the model the preview mapper for the entity type built (e.g. <see cref="SyncDataTypePreview"/>).
    /// </summary>
    public object? Preview { get; set; }

    public List<uSyncChange> Details { get; set; } = [];
}
