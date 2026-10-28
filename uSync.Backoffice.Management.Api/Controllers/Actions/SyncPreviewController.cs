using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Api.Management.ViewModels.DataType;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class SyncPreviewController : uSyncControllerBase
{
    private readonly ISyncPreviewService _previewService;
    private readonly IUmbracoMapper _umbracoMapper;

    public SyncPreviewController(ISyncPreviewService previewService, IUmbracoMapper umbracoMapper)
    {
        _previewService = previewService;
        _umbracoMapper = umbracoMapper;
    }

    [HttpGet("Preview")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SyncItemPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview(string handler, Guid key)
    {
        var result = await _previewService.GetPreviewAsync(handler, key, MapPreview);
        return result is null ? NotFound() : Ok(result);
    }

    private SyncItemPreviewResponse MapPreview(SyncItemPreview preview)
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

        if (preview.Current is IDataType || preview.Incoming is IDataType)
        {
            response.EntityType = Umbraco.Cms.Core.Constants.UdiEntityType.DataType;
            response.CurrentDataType = MapItem<IDataType, DataTypeResponseModel>(preview.Current);
            response.IncomingDataType = MapItem<IDataType, DataTypeResponseModel>(preview.Incoming);
        }

        return response;
    }

    private TResponse? MapItem<TItem, TResponse>(object? item)
        where TResponse : class
        => item is TItem typed ? _umbracoMapper.Map<TResponse>(typed) : null;
}
