using System;
using System.Collections.Generic;
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
    Task<SyncActionResult> ImportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks);

    /// <summary>
    ///  run the post import step at the end of an import 
    /// </summary>
    [Obsolete("use ImportPostAsync will be removed in v16")]
    SyncActionResult ImportPost(SyncActionOptions options, uSyncCallbacks? callbacks)
        => ImportPostAsync(options, callbacks).Result;
    Task<SyncActionResult> ImportPostAsync(SyncActionOptions options, uSyncCallbacks? callbacks);

    /// <summary>
    ///  run a report for a given handler based on the options provided.
    /// </summary>
    [Obsolete("use ReportHandlerAsync will be removed in v16")]
    SyncActionResult ReportHandler(SyncActionOptions options, uSyncCallbacks? callbacks)
        => ReportHandlerAsync(options, callbacks).Result;
    Task<SyncActionResult> ReportHandlerAsync(SyncActionOptions options, uSyncCallbacks? callbacks);

    /// <summary>
    ///  start the bulk process
    /// </summary>
    [Obsolete("use StartProcessAsync will be removed in v16")]
    void StartProcess(HandlerActions action) => StartProcessAsync(action).Wait();
    Task StartProcessAsync(HandlerActions action);

    /// <summary>
    ///  finish the bulk process
    /// </summary>
    Task FinishProcessAsync(HandlerActions action, IEnumerable<uSyncAction> actions, string username);
}