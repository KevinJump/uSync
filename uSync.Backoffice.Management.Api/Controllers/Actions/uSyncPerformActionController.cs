using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

using System.Net.Mime;
using System.Text;

using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

using uSync.Backoffice.Management.Api.Models;
using uSync.Backoffice.Management.Api.Services;
using uSync.BackOffice.Models;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class uSyncPerformActionController : uSyncControllerBase
{
    private readonly ISyncManagementService _managementService;
    private readonly ITemporaryFileService _temporaryFileService;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    public uSyncPerformActionController(
        ISyncManagementService managementService,
        ITemporaryFileService temporaryFileService,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _managementService = managementService;
        _temporaryFileService = temporaryFileService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    [HttpPost("Perform")]
    [ProducesResponseType(typeof(PerformActionResponse), 200)]
    public async Task<PerformActionResponse> PerformAction(PerformActionRequest model)
        => await _managementService.PerformActionAsync(model, _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser);

    /// <summary>
    ///  status of a background operation - lets the client poll for progress,
    ///  or reattach to a run it did not itself start (e.g. after a page reload).
    /// </summary>
    [HttpGet("Status")]
    [ProducesResponseType(typeof(SyncOperationStatusResponse), 200)]
    public async Task<SyncOperationStatusResponse> Status(Guid operationId)
        => await _managementService.GetOperationStatusAsync(operationId);

    /// <summary>
    ///  the run (if any) currently active on the server.
    /// </summary>
    [HttpGet("Running")]
    [ProducesResponseType(typeof(SyncRunningOperationResponse), 200)]
    public async Task<SyncRunningOperationResponse> Running()
        => await _managementService.GetRunningOperationAsync();


    [HttpPost("Download")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    [Authorize(Policy = AuthorizationPolicies.RequireAdminAccess)]
    public ActionResult Download()
    {
        var filename = "uSync.zip";

        var stream = _managementService.CompressExportFolder();
        if (stream is null) return NotFound();

        var mediaType = new MediaTypeHeaderValue(MediaTypeNames.Application.Zip)
        {
            Charset = Encoding.UTF8.WebName
        };

        var contentDisposition = new ContentDisposition
        {
            FileName = filename,
            DispositionType = DispositionTypeNames.Attachment
        };

        Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

        return new FileStreamResult(stream, mediaType)
        {
            FileDownloadName = filename
        };
    }

    [HttpPost("ProcessUpload")]
    [ProducesResponseType<UploadImportResult>(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(NotFoundResult))]
    public async Task<IActionResult> ProcessUpload(Guid tempKey)
    {
        var tempFile = await _temporaryFileService.GetAsync(tempKey);
        if (tempFile is null) return NotFound();

        using (var stream = tempFile.OpenReadStream())
        {
            return Ok(await _managementService.UnpackStreamAsync(stream));
        }
    }
}
