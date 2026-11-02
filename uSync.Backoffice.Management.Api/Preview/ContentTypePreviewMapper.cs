using Umbraco.Cms.Api.Management.ViewModels.DocumentType;
using Umbraco.Cms.Api.Management.ViewModels.MediaType;
using Umbraco.Cms.Api.Management.ViewModels.MemberType;
using Umbraco.Cms.Core.Models;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice.Models;
using uSync.BackOffice.Preview;

using UdiEntityType = Umbraco.Cms.Core.Constants.UdiEntityType;

namespace uSync.Backoffice.Management.Api.Preview;

/// <summary>
///  maps document, media and member types, with their compositions and data types.
/// </summary>
public abstract class ContentTypePreviewMapperBase<TItem, TTypeModel> : SyncPreviewMapperBase<TItem>
    where TItem : class, IContentTypeComposition
    where TTypeModel : class
{
    private readonly ISyncPreviewMappingHelper _helper;

    protected ContentTypePreviewMapperBase(ISyncPreviewMappingHelper helper)
    {
        _helper = helper;
    }

    protected override async Task<object?> MapAsync(SyncItemPreview preview, TItem? current, TItem? incoming)
    {
        IContentTypeComposition[] types = [.. new[] { current, incoming }.OfType<TItem>()];
        var (dataTypes, newDataTypes) = await _helper.MapDataTypesAsync(types, preview);

        return new SyncContentTypePreview<TTypeModel>
        {
            Current = _helper.Map<TItem, TTypeModel>(current),
            Incoming = _helper.Map<TItem, TTypeModel>(incoming),
            Compositions = _helper.MapCompositions<TItem, TTypeModel>(types),
            DataTypes = dataTypes,
            NewDataTypes = newDataTypes,
            NewCompositions = _helper.GetNewCompositions(preview),
        };
    }
}

public class DocumentTypePreviewMapper : ContentTypePreviewMapperBase<IContentType, DocumentTypeResponseModel>
{
    public DocumentTypePreviewMapper(ISyncPreviewMappingHelper helper)
        : base(helper)
    { }

    public override string EntityType => UdiEntityType.DocumentType;
}

public class MediaTypePreviewMapper : ContentTypePreviewMapperBase<IMediaType, MediaTypeResponseModel>
{
    public MediaTypePreviewMapper(ISyncPreviewMappingHelper helper)
        : base(helper)
    { }

    public override string EntityType => UdiEntityType.MediaType;
}

public class MemberTypePreviewMapper : ContentTypePreviewMapperBase<IMemberType, MemberTypeResponseModel>
{
    public MemberTypePreviewMapper(ISyncPreviewMappingHelper helper)
        : base(helper)
    { }

    public override string EntityType => UdiEntityType.MemberType;
}
