using Umbraco.Cms.Api.Management.ViewModels.DataType;

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
    public string EntityType { get; set; } = string.Empty;

    public bool IsDelete { get; set; }

    public bool Success { get; set; }

    public string? Message { get; set; }

    public DataTypeResponseModel? CurrentDataType { get; set; }

    public DataTypeResponseModel? IncomingDataType { get; set; }

    public List<uSyncChange> Details { get; set; } = [];
}
