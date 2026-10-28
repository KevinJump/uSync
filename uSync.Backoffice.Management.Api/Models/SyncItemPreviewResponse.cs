using Umbraco.Cms.Api.Management.ViewModels.DataType;
using Umbraco.Cms.Api.Management.ViewModels.DocumentType;
using Umbraco.Cms.Api.Management.ViewModels.MediaType;
using Umbraco.Cms.Api.Management.ViewModels.MemberType;

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

    public DocumentTypeResponseModel? CurrentDocumentType { get; set; }

    public DocumentTypeResponseModel? IncomingDocumentType { get; set; }

    public MediaTypeResponseModel? CurrentMediaType { get; set; }

    public MediaTypeResponseModel? IncomingMediaType { get; set; }

    public MemberTypeResponseModel? CurrentMemberType { get; set; }

    public MemberTypeResponseModel? IncomingMemberType { get; set; }

    /// <summary>
    ///  the content types either side is composed of (as they are in the site), 
    ///  so the preview can show inherited tabs, groups and properties.
    /// </summary>
    public List<DocumentTypeResponseModel> CompositionDocumentTypes { get; set; } = [];

    public List<MediaTypeResponseModel> CompositionMediaTypes { get; set; } = [];

    public List<MemberTypeResponseModel> CompositionMemberTypes { get; set; } = [];

    /// <summary>
    ///  the data types used by the properties on either side (as they are in the site).
    /// </summary>
    public List<DataTypeResponseModel> DataTypes { get; set; } = [];

    public List<uSyncChange> Details { get; set; } = [];
}
