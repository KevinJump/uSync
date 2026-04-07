using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice.Services;

/// <summary>
///  handling actions (imports, exports, reports, etc)
/// </summary>
public interface ISyncActionService
{
    /// <summary>
    ///  remove all the files from the export folder 
    /// </summary>
    void CleanExportFolder();

    /// <summary>
    ///  run an export based on the options provided
    /// </summary>
    Task<SyncActionResult> ExportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks);

    /// <summary>
    ///  get a list of the handlers for a given action
    /// </summary>
    IEnumerable<SyncHandlerView> GetActionHandlers(HandlerActions action, uSyncOptions? options);

    /// <summary>
    ///  run an export based on the options provided
    /// </summary>
    Task<SyncActionResult> ImportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks);

    /// <summary>
    ///  run an export based on the options provided
    /// </summary>
    Task<SyncActionResult> ImportPostAsync(SyncFinalActionRequest request);

    /// <summary>
    ///  run a report for a given handler based on the options provided.
    /// </summary>
    Task<SyncActionResult> ReportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks);

    /// <summary>
    ///  run an export based on the options provided
    /// </summary>
    [Obsolete("Use StartProcessAsync(SyncStartActionRequest request) will be removed in v18")]
    Task StartProcessAsync(HandlerActions action);

    /// <summary>
    ///  run an export based on the options provided
    /// </summary>
    Task StartProcessAsync(SyncStartActionRequest request);

    /// <summary>
    ///  finish the bulk process
    /// </summary>
    Task<SyncActionResult> FinishProcessAsync(SyncFinalActionRequest request);

    /// <summary>
    ///  returns the export folder zipped up as a stream
    /// </summary>
    /// <returns></returns>
    Stream GetExportFolderAsStream();

    /// <summary>
    ///  unpacks a zip archive (stream) to disk, checks it and copies it over the existing uSync folder. 
    /// </summary>
    [Obsolete("Use UnpackImportFromStreamAsync(Stream stream) will be removed in v19")]
    UploadImportResult UnpackImportFromStream(Stream stream)
        => UnpackImportFromStreamAsync(stream).GetAwaiter().GetResult();

    /// <summary>
    ///  unpacks a zip archive (stream) to disk, checks it and copies it over the existing uSync folder. 
    /// </summary>
    Task<UploadImportResult> UnpackImportFromStreamAsync(Stream stream);
}