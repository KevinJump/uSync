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
    [Obsolete("use ExportHandlerAsync will be removed in v16")]
    SyncActionResult ExportHandler(SyncActionOptions options, uSyncCallbacks? callbacks)
        => ExportHandlerAsync(options, callbacks).Result;

    /// <summary>
    ///  run an export based on the options provided
    /// </summary>
    Task<SyncActionResult> ExportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks);

    /// <summary>
    ///  get a list of the handlers for a given action
    /// </summary>
    IEnumerable<SyncHandlerView> GetActionHandlers(HandlerActions action, uSyncOptions? options);

    /// <summary>
    ///  run an import against a handler based on the options provided.
    /// </summary>
    [Obsolete("use ImportHandlerAsync will be removed in v16")]
    SyncActionResult ImportHandler(SyncActionOptions options, uSyncCallbacks? callbacks)
        => ImportHandlerAsync(options, callbacks).Result;

    /// <summary>
    ///  run an export based on the options provided
    /// </summary>
    Task<SyncActionResult> ImportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks);

    /// <summary>
    ///  run the post import step at the end of an import 
    /// </summary>
    [Obsolete("use ImportPostAsync will be removed in v16")]
    SyncActionResult ImportPost(SyncActionOptions options, uSyncCallbacks? callbacks)
        => ImportPostAsync(options, callbacks).Result;

    /// <summary>
    ///  run an export based on the options provided
    /// </summary>
    [Obsolete("use ImportPostAsync with finalActionRequest , will be removed in v16")]
    Task<SyncActionResult> ImportPostAsync(SyncActionOptions options, uSyncCallbacks? callbacks)
        => ImportPostAsync(new SyncFinalActionRequest { RequestId = Guid.NewGuid(), ActionOptions = options, Callbacks = callbacks });

    /// <summary>
    ///  run an export based on the options provided
    /// </summary>
    Task<SyncActionResult> ImportPostAsync(SyncFinalActionRequest request);

    /// <summary>
    ///  run a report for a given handler based on the options provided.
    /// </summary>
    [Obsolete("use ReportHandlerAsync will be removed in v16")]
    SyncActionResult ReportHandler(SyncActionOptions options, uSyncCallbacks? callbacks)
        => ReportHandlerAsync(options, callbacks).Result;

    /// <summary>
    ///  run a report for a given handler based on the options provided.
    /// </summary>
    Task<SyncActionResult> ReportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks);

    /// <summary>
    ///  start the bulk process
    /// </summary>
    [Obsolete("use StartProcessAsync will be removed in v16")]
    void StartProcess(HandlerActions action) => StartProcessAsync(action).Wait();

    /// <summary>
    ///  run an export based on the options provided
    /// </summary>
    Task StartProcessAsync(HandlerActions action);

    /// <summary>
    ///  finish the bulk process
    /// </summary>
    [Obsolete("use FinishProcessAsync with FinalActionRequest will be removed in v16")]
    Task FinishProcessAsync(HandlerActions action, IEnumerable<uSyncAction> actions, string username)
        => FinishProcessAsync(new SyncFinalActionRequest
        {
            HandlerAction = action,
            RequestId = Guid.NewGuid(),
            ActionOptions = new(),
            Actions = actions,
            Username = username
        });

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
    UploadImportResult UnpackImportFromStream(Stream stream);
}