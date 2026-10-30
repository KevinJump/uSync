using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice.Services;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class SyncPreviewController : uSyncControllerBase
{
    private readonly ISyncPreviewService _previewService;

    public SyncPreviewController(ISyncPreviewService previewService)
    {
        _previewService = previewService;
    }

    [HttpGet("Preview")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(SyncItemPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreview(string handler, Guid key)
    {
        var result = await _previewService.GetPreviewAsync(handler, key);
        if (result is null) return NotFound();

        var preview = result.Preview;
        return Ok(new SyncItemPreviewResponse
        {
            Key = preview.Key,
            Handler = preview.Handler,
            ItemType = preview.ItemType,
            EntityType = result.EntityType,
            IsDelete = preview.IsDelete,
            Success = preview.Success,
            Message = preview.Message,
            Preview = result.Model,
            Details = [.. preview.Details],
        });
    }
}
