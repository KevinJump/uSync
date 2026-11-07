using Umbraco.Cms.Api.Management.ViewModels.DataType;

namespace uSync.Backoffice.Management.Api.Models;

/// <summary>
///  preview of a data type (entity type data-type).
/// </summary>
public class SyncDataTypePreview
{
    public DataTypeResponseModel? Current { get; set; }

    public DataTypeResponseModel? Incoming { get; set; }
}

/// <summary>
///  what the preview needs to lay out the properties of an item, as the editor shows them.
/// </summary>
/// <typeparam name="TTypeModel">document, media or member type model</typeparam>
public abstract class SyncPropertiesPreview<TTypeModel>
{
    /// <summary>
    ///  the content types either side is composed of (as they are in the site),
    ///  so the preview can show inherited tabs, groups and properties.
    /// </summary>
    public List<TTypeModel> Compositions { get; set; } = [];

    /// <summary>
    ///  the data types used by the properties on either side (as they are in the site).
    /// </summary>
    public List<DataTypeResponseModel> DataTypes { get; set; } = [];

    /// <summary>
    ///  keys of the data types in <see cref="DataTypes"/> that aren't in the site yet (built from the uSync files).
    /// </summary>
    public List<Guid> NewDataTypes { get; set; } = [];

    /// <summary>
    ///  keys of the compositions that aren't in the site yet (built from the uSync files).
    /// </summary>
    public List<Guid> NewCompositions { get; set; } = [];
}

/// <summary>
///  preview of a document, media or member type (entity type document-type, media-type, member-type).
/// </summary>
public class SyncContentTypePreview<TTypeModel> : SyncPropertiesPreview<TTypeModel>
{
    public TTypeModel? Current { get; set; }

    public TTypeModel? Incoming { get; set; }
}

/// <summary>
///  preview of a document or media item (entity type document, media).
/// </summary>
public class SyncContentPreview<TModel, TTypeModel> : SyncPropertiesPreview<TTypeModel>
{
    public TModel? Current { get; set; }

    public TModel? Incoming { get; set; }

    /// <summary>
    ///  parent, sort order, template and published state of the item.
    /// </summary>
    public SyncContentPreviewInfo? CurrentInfo { get; set; }

    public SyncContentPreviewInfo? IncomingInfo { get; set; }

    /// <summary>
    ///  the document (or media) type of the item, as it is in the site.
    /// </summary>
    public TTypeModel? ContentType { get; set; }
}
