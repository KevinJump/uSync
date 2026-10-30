using Umbraco.Cms.Api.Management.ViewModels.Document;
using Umbraco.Cms.Api.Management.ViewModels.DocumentType;
using Umbraco.Cms.Api.Management.ViewModels.Media;
using Umbraco.Cms.Api.Management.ViewModels.MediaType;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice.Models;
using uSync.BackOffice.Preview;

using UdiEntityType = Umbraco.Cms.Core.Constants.UdiEntityType;

namespace uSync.Backoffice.Management.Api.Preview;

/// <summary>
///  maps documents and media, with the info that isn't a property value
///  and the content type (as it is in the site) to lay out the properties.
/// </summary>
public abstract class ContentPreviewMapperBase<TItem, TModel, TType, TTypeModel> : SyncPreviewMapperBase<TItem>
    where TItem : class, IContentBase
    where TModel : class
    where TType : class, IContentTypeComposition
    where TTypeModel : class
{
    private readonly ISyncPreviewMappingHelper _helper;

    protected ContentPreviewMapperBase(ISyncPreviewMappingHelper helper)
    {
        _helper = helper;
    }

    protected abstract UmbracoObjectTypes ObjectType { get; }

    /// <summary>
    ///  the content type of the item, from the site.
    /// </summary>
    protected abstract TType? GetContentType(TItem item);

    protected override async Task<object?> MapAsync(SyncItemPreview preview, TItem? current, TItem? incoming)
    {
        var result = new SyncContentPreview<TModel, TTypeModel>
        {
            Current = _helper.Map<TItem, TModel>(current),
            Incoming = _helper.Map<TItem, TModel>(incoming),
            CurrentInfo = await _helper.MapContentInfoAsync(current, ObjectType, null, preview.Options),
            IncomingInfo = await _helper.MapContentInfoAsync(incoming, ObjectType, preview.Node, preview.Options),
            NewCompositions = _helper.GetNewCompositions(preview),
        };

        // the incoming item's type, if they differ.
        var item = incoming ?? current;
        var contentType = item is null ? null : GetContentType(item);
        if (contentType is not null)
        {
            result.ContentType = _helper.Map<TType, TTypeModel>(contentType);
            result.Compositions = _helper.MapCompositions<TType, TTypeModel>([contentType]);
            (result.DataTypes, result.NewDataTypes) = await _helper.MapDataTypesAsync([contentType], preview);
        }

        return result;
    }
}

public class DocumentPreviewMapper : ContentPreviewMapperBase<IContent, DocumentResponseModel, IContentType, DocumentTypeResponseModel>
{
    private readonly IContentTypeService _contentTypeService;

    public DocumentPreviewMapper(ISyncPreviewMappingHelper helper, IContentTypeService contentTypeService)
        : base(helper)
    {
        _contentTypeService = contentTypeService;
    }

    public override string EntityType => UdiEntityType.Document;

    protected override UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Document;

    protected override IContentType? GetContentType(IContent item)
        => _contentTypeService.Get(item.ContentType.Key);
}

public class MediaPreviewMapper : ContentPreviewMapperBase<IMedia, MediaResponseModel, IMediaType, MediaTypeResponseModel>
{
    private readonly IMediaTypeService _mediaTypeService;

    public MediaPreviewMapper(ISyncPreviewMappingHelper helper, IMediaTypeService mediaTypeService)
        : base(helper)
    {
        _mediaTypeService = mediaTypeService;
    }

    public override string EntityType => UdiEntityType.Media;

    protected override UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Media;

    protected override IMediaType? GetContentType(IMedia item)
        => _mediaTypeService.Get(item.ContentType.Key);
}
