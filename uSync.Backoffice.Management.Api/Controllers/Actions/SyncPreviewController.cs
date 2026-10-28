using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Api.Management.ViewModels.DataType;
using Umbraco.Cms.Api.Management.ViewModels.DocumentType;
using Umbraco.Cms.Api.Management.ViewModels.MediaType;
using Umbraco.Cms.Api.Management.ViewModels.MemberType;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;

using UdiEntityType = Umbraco.Cms.Core.Constants.UdiEntityType;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class SyncPreviewController : uSyncControllerBase
{
    private readonly ISyncPreviewService _previewService;
    private readonly IUmbracoMapper _umbracoMapper;
    private readonly IDataTypeService _dataTypeService;

    public SyncPreviewController(
        ISyncPreviewService previewService,
        IUmbracoMapper umbracoMapper,
        IDataTypeService dataTypeService)
    {
        _previewService = previewService;
        _umbracoMapper = umbracoMapper;
        _dataTypeService = dataTypeService;
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
                response.CompositionDocumentTypes = MapCompositions<IContentType, DocumentTypeResponseModel>(preview);
                response.DataTypes = await MapDataTypesAsync(preview);
                break;

            case IMediaType:
                response.EntityType = UdiEntityType.MediaType;
                response.CurrentMediaType = MapItem<IMediaType, MediaTypeResponseModel>(preview.Current);
                response.IncomingMediaType = MapItem<IMediaType, MediaTypeResponseModel>(preview.Incoming);
                response.CompositionMediaTypes = MapCompositions<IMediaType, MediaTypeResponseModel>(preview);
                response.DataTypes = await MapDataTypesAsync(preview);
                break;

            case IMemberType:
                response.EntityType = UdiEntityType.MemberType;
                response.CurrentMemberType = MapItem<IMemberType, MemberTypeResponseModel>(preview.Current);
                response.IncomingMemberType = MapItem<IMemberType, MemberTypeResponseModel>(preview.Incoming);
                response.CompositionMemberTypes = MapCompositions<IMemberType, MemberTypeResponseModel>(preview);
                response.DataTypes = await MapDataTypesAsync(preview);
                break;
        }

        return response;
    }

    private TResponse? MapItem<TItem, TResponse>(object? item)
        where TResponse : class
        => item is TItem typed ? _umbracoMapper.Map<TResponse>(typed) : null;

    /// <summary>
    ///  every content type either side is composed of (including compositions of compositions).
    /// </summary>
    private List<TResponse> MapCompositions<TItem, TResponse>(SyncItemPreview preview)
        where TItem : IContentTypeComposition
        where TResponse : class
    {
        var found = new Dictionary<Guid, TItem>();
        var queue = new Queue<IContentTypeComposition>(
            ContentTypes(preview).SelectMany(x => x.ContentTypeComposition));

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
    private async Task<List<DataTypeResponseModel>> MapDataTypesAsync(SyncItemPreview preview)
    {
        var keys = ContentTypes(preview)
            .SelectMany(x => x.CompositionPropertyTypes)
            .Select(x => x.DataTypeKey)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (keys.Length == 0) return [];

        var dataTypes = await _dataTypeService.GetAllAsync(keys);
        return dataTypes
            .Select(x => _umbracoMapper.Map<DataTypeResponseModel>(x))
            .OfType<DataTypeResponseModel>()
            .ToList();
    }

    private static IEnumerable<IContentTypeComposition> ContentTypes(SyncItemPreview preview)
        => new[] { preview.Current, preview.Incoming }.OfType<IContentTypeComposition>();
}
