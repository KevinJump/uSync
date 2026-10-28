using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Xml.Linq;

using Umbraco.Cms.Api.Management.ViewModels.DataType;
using Umbraco.Cms.Api.Management.ViewModels.Document;
using Umbraco.Cms.Api.Management.ViewModels.DocumentType;
using Umbraco.Cms.Api.Management.ViewModels.Media;
using Umbraco.Cms.Api.Management.ViewModels.MediaType;
using Umbraco.Cms.Api.Management.ViewModels.MemberType;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Extensions;

using UdiEntityType = Umbraco.Cms.Core.Constants.UdiEntityType;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class SyncPreviewController : uSyncControllerBase
{
    private readonly ISyncPreviewService _previewService;
    private readonly IUmbracoMapper _umbracoMapper;
    private readonly IDataTypeService _dataTypeService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IEntityService _entityService;
    private readonly ITemplateService _templateService;

    public SyncPreviewController(
        ISyncPreviewService previewService,
        IUmbracoMapper umbracoMapper,
        IDataTypeService dataTypeService,
        IContentTypeService contentTypeService,
        IMediaTypeService mediaTypeService,
        IEntityService entityService,
        ITemplateService templateService)
    {
        _previewService = previewService;
        _umbracoMapper = umbracoMapper;
        _dataTypeService = dataTypeService;
        _contentTypeService = contentTypeService;
        _mediaTypeService = mediaTypeService;
        _entityService = entityService;
        _templateService = templateService;
    }

    [HttpGet("Preview")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SyncItemPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview(string handler, Guid key)
    {
        var result = await _previewService.GetPreviewAsync(handler, key, MapPreviewAsync);
        return result is null ? NotFound() : Ok(result);
    }

    private async Task<SyncItemPreviewResponse> MapPreviewAsync(SyncItemPreview preview)
    {
        var response = new SyncItemPreviewResponse
        {
            Key = preview.Key,
            Handler = preview.Handler,
            ItemType = preview.ItemType,
            IsDelete = preview.IsDelete,
            Success = preview.Success,
            Message = preview.Message,
            Details = [.. preview.Details],
        };

        switch (preview.Current ?? preview.Incoming)
        {
            case IDataType:
                response.EntityType = UdiEntityType.DataType;
                response.CurrentDataType = MapItem<IDataType, DataTypeResponseModel>(preview.Current);
                response.IncomingDataType = MapItem<IDataType, DataTypeResponseModel>(preview.Incoming);
                break;

            case IContentType:
                response.EntityType = UdiEntityType.DocumentType;
                response.CurrentDocumentType = MapItem<IContentType, DocumentTypeResponseModel>(preview.Current);
                response.IncomingDocumentType = MapItem<IContentType, DocumentTypeResponseModel>(preview.Incoming);
                response.CompositionDocumentTypes = MapCompositions<IContentType, DocumentTypeResponseModel>(ContentTypes(preview));
                await MapDataTypesAsync(ContentTypes(preview), preview, response);
                break;

            case IMediaType:
                response.EntityType = UdiEntityType.MediaType;
                response.CurrentMediaType = MapItem<IMediaType, MediaTypeResponseModel>(preview.Current);
                response.IncomingMediaType = MapItem<IMediaType, MediaTypeResponseModel>(preview.Incoming);
                response.CompositionMediaTypes = MapCompositions<IMediaType, MediaTypeResponseModel>(ContentTypes(preview));
                await MapDataTypesAsync(ContentTypes(preview), preview, response);
                break;

            case IMemberType:
                response.EntityType = UdiEntityType.MemberType;
                response.CurrentMemberType = MapItem<IMemberType, MemberTypeResponseModel>(preview.Current);
                response.IncomingMemberType = MapItem<IMemberType, MemberTypeResponseModel>(preview.Incoming);
                response.CompositionMemberTypes = MapCompositions<IMemberType, MemberTypeResponseModel>(ContentTypes(preview));
                await MapDataTypesAsync(ContentTypes(preview), preview, response);
                break;

            case IContent:
                response.EntityType = UdiEntityType.Document;
                response.CurrentDocument = MapItem<IContent, DocumentResponseModel>(preview.Current);
                response.IncomingDocument = MapItem<IContent, DocumentResponseModel>(preview.Incoming);
                response.CurrentInfo = await MapContentInfoAsync(preview.Current as IContent, UmbracoObjectTypes.Document, null);
                response.IncomingInfo = await MapContentInfoAsync(preview.Incoming as IContent, UmbracoObjectTypes.Document, preview.Node);

                var contentType = ContentTypeOf<IContent, IContentType>(preview, x => _contentTypeService.Get(x.ContentType.Key));
                if (contentType is not null)
                {
                    response.CurrentDocumentType = _umbracoMapper.Map<DocumentTypeResponseModel>(contentType);
                    response.CompositionDocumentTypes = MapCompositions<IContentType, DocumentTypeResponseModel>([contentType]);
                    await MapDataTypesAsync([contentType], preview, response);
                }
                break;

            case IMedia:
                response.EntityType = UdiEntityType.Media;
                response.CurrentMedia = MapItem<IMedia, MediaResponseModel>(preview.Current);
                response.IncomingMedia = MapItem<IMedia, MediaResponseModel>(preview.Incoming);
                response.CurrentInfo = await MapContentInfoAsync(preview.Current as IMedia, UmbracoObjectTypes.Media, null);
                response.IncomingInfo = await MapContentInfoAsync(preview.Incoming as IMedia, UmbracoObjectTypes.Media, preview.Node);

                var mediaType = ContentTypeOf<IMedia, IMediaType>(preview, x => _mediaTypeService.Get(x.ContentType.Key));
                if (mediaType is not null)
                {
                    response.CurrentMediaType = _umbracoMapper.Map<MediaTypeResponseModel>(mediaType);
                    response.CompositionMediaTypes = MapCompositions<IMediaType, MediaTypeResponseModel>([mediaType]);
                    await MapDataTypesAsync([mediaType], preview, response);
                }
                break;
        }

        response.NewCompositions = [.. preview.PendingItems
            .OfType<IContentTypeComposition>()
            .Select(x => x.Key)];

        return response;
    }

    private TResponse? MapItem<TItem, TResponse>(object? item)
        where TResponse : class
        => item is TItem typed ? _umbracoMapper.Map<TResponse>(typed) : null;

    /// <summary>
    ///  every content type either side is composed of (including compositions of compositions).
    /// </summary>
    private List<TResponse> MapCompositions<TItem, TResponse>(IEnumerable<IContentTypeComposition> types)
        where TItem : IContentTypeComposition
        where TResponse : class
    {
        var found = new Dictionary<Guid, TItem>();
        var queue = new Queue<IContentTypeComposition>(
            types.SelectMany(x => x.ContentTypeComposition));

        while (queue.TryDequeue(out var composition))
        {
            if (composition is not TItem typed || found.ContainsKey(typed.Key)) continue;
            found[typed.Key] = typed;

            foreach (var child in typed.ContentTypeComposition)
                queue.Enqueue(child);
        }

        return found.Values
            .Select(x => _umbracoMapper.Map<TResponse>(x))
            .OfType<TResponse>()
            .ToList();
    }

    /// <summary>
    ///  the data types used by any property on either side, including inherited ones.
    /// </summary>
    /// <remarks>
    ///  data types that aren't in the site yet are built from the uSync files, 
    ///  so new properties can still show their editor.
    /// </remarks>
    private async Task MapDataTypesAsync(IEnumerable<IContentTypeComposition> types, SyncItemPreview preview, SyncItemPreviewResponse response)
    {
        var keys = types
            .SelectMany(x => x.CompositionPropertyTypes)
            .Select(x => x.DataTypeKey)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (keys.Length == 0) return;

        var dataTypes = (await _dataTypeService.GetAllAsync(keys)).ToList();

        // data types that aren't in the site yet were built from the uSync files during the preview.
        var pending = preview.PendingItems
            .OfType<IDataType>()
            .Where(x => keys.Contains(x.Key) && dataTypes.All(d => d.Key != x.Key))
            .ToList();

        if (pending.Count > 0)
        {
            dataTypes.AddRange(pending);
            response.NewDataTypes = [.. pending.Select(x => x.Key)];
        }

        response.DataTypes = dataTypes
            .Select(x => _umbracoMapper.Map<DataTypeResponseModel>(x))
            .OfType<DataTypeResponseModel>()
            .ToList();
    }

    private static IEnumerable<IContentTypeComposition> ContentTypes(SyncItemPreview preview)
        => new[] { preview.Current, preview.Incoming }.OfType<IContentTypeComposition>();

    /// <summary>
    ///  the content (or media) type of the item, the incoming one's if they differ.
    /// </summary>
    private static TType? ContentTypeOf<TItem, TType>(SyncItemPreview preview, Func<TItem, TType?> getType)
        where TType : class
    {
        var item = preview.Incoming is TItem incoming ? incoming
            : preview.Current is TItem current ? current
            : default;

        return item is null ? null : getType(item);
    }

    private async Task<SyncContentPreviewInfo?> MapContentInfoAsync(IContentBase? item, UmbracoObjectTypes objectType, XElement? node)
    {
        if (item is null) return null;

        var info = new SyncContentPreviewInfo
        {
            Parent = GetParentName(item, objectType, node),
            // content sets the sort order in the second pass (which a preview doesn't run), so read it from the file.
            SortOrder = node?.Element(uSyncConstants.Xml.Info)?.Element(uSyncConstants.Xml.SortOrder).ValueOrDefault(item.SortOrder) ?? item.SortOrder,
            Trashed = item.Trashed,
        };

        if (item is IContent content)
        {
            if (content.TemplateId is int templateId)
                info.Template = (await _templateService.GetAsync(templateId))?.Alias;

            info.Published = node is null
                ? GetPublishedState(content)
                : GetPublishedState(node);
        }

        return info;
    }

    /// <summary>
    ///  the parent's name, from the site or (for a parent that isn't in the site yet) the uSync file.
    /// </summary>
    private string? GetParentName(IContentBase item, UmbracoObjectTypes objectType, XElement? node)
    {
        if (item.ParentId > 0)
            return _entityService.Get(item.ParentId, objectType)?.Name;

        var parentNode = node?.Element(uSyncConstants.Xml.Info)?.Element(uSyncConstants.Xml.Parent);
        if (parentNode is not null && parentNode.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty) != Guid.Empty)
            return parentNode.ValueOrDefault(string.Empty);

        return null;
    }

    private static List<SyncContentPreviewCulture> GetPublishedState(IContent content)
        => content.ContentType.VariesByCulture()
            ? [.. content.AvailableCultures.Order().Select(culture => new SyncContentPreviewCulture
            {
                Culture = culture,
                Published = content.IsCulturePublished(culture),
            })]
            : [new SyncContentPreviewCulture { Published = content.Published }];

    /// <summary>
    ///  the published state the uSync file will leave the item in.
    /// </summary>
    private static List<SyncContentPreviewCulture> GetPublishedState(XElement node)
    {
        var published = node.Element(uSyncConstants.Xml.Info)?.Element("Published");
        if (published is null) return [];

        if (published.HasElements)
        {
            return [.. published.Elements("Published")
                .Select(x => new SyncContentPreviewCulture
                {
                    Culture = x.Attribute("Culture").ValueOrDefault(string.Empty),
                    Published = x.ValueOrDefault(false),
                })
                .OrderBy(x => x.Culture)];
        }

        return [new SyncContentPreviewCulture { Published = published.Attribute("Default").ValueOrDefault(false) }];
    }
}
