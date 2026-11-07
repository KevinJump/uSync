using System.Xml.Linq;

using Umbraco.Cms.Api.Management.ViewModels.DataType;
using Umbraco.Cms.Core.Models;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice.Models;
using uSync.Core.Serialization;

namespace uSync.Backoffice.Management.Api.Preview;

/// <summary>
///  the mapping the preview mappers share, so items with properties
///  can show their compositions and the editors for their data types.
/// </summary>
public interface ISyncPreviewMappingHelper
{
    /// <summary>
    ///  map an item to the backoffice model, when it is the type expected (null if it isn't).
    /// </summary>
    TResponse? Map<TItem, TResponse>(object? item)
        where TResponse : class;

    /// <summary>
    ///  every content type the types are composed of (including compositions of compositions).
    /// </summary>
    List<TResponse> MapCompositions<TItem, TResponse>(IEnumerable<IContentTypeComposition> types)
        where TItem : IContentTypeComposition
        where TResponse : class;

    /// <summary>
    ///  keys of the compositions that aren't in the site yet (built from the uSync files).
    /// </summary>
    List<Guid> GetNewCompositions(SyncItemPreview preview);

    /// <summary>
    ///  the data types used by any property on the types (including inherited ones), and
    ///  the keys of the ones that aren't in the site yet.
    /// </summary>
    Task<(List<DataTypeResponseModel> DataTypes, List<Guid> NewDataTypes)> MapDataTypesAsync(
        IEnumerable<IContentTypeComposition> types, SyncItemPreview preview);

    /// <summary>
    ///  parent, sort order, trashed, template and published state of a content or media item.
    /// </summary>
    /// <remarks>
    ///  pass the uSync file (and options) for the incoming item, to read the values
    ///  the import sets after the item is saved.
    /// </remarks>
    Task<SyncContentPreviewInfo?> MapContentInfoAsync(IContentBase? item, UmbracoObjectTypes objectType, XElement? node, SyncSerializerOptions? options);
}
