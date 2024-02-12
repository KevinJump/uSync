using System.Collections.Generic;

using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers;

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
    SyncActionResult ExportHandler(SyncActionOptions options, uSyncCallbacks callbacks);

    /// <summary>
    ///  get a list of the handlers for a given action
    /// </summary>
    IEnumerable<SyncHandlerView> GetActionHandlers(HandlerActions action, uSyncOptions options);

    /// <summary>
    ///  run an import against a handler based on the options provided.
    /// </summary>
    SyncActionResult ImportHandler(SyncActionOptions options, uSyncCallbacks callbacks);

    /// <summary>
    ///  run the post import step at the end of an import 
    /// </summary>
    SyncActionResult ImportPost(SyncActionOptions options, uSyncCallbacks callbacks);

    /// <summary>
    ///  run a report for a given handler based on the options provided.
    /// </summary>
    SyncActionResult ReportHandler(SyncActionOptions options, uSyncCallbacks callbacks);

    /// <summary>
    ///  start the bulk process
    /// </summary>
    void StartProcess(HandlerActions action);

    /// <summary>
    ///  finish the bulk process
    /// </summary>
    void FinishProcess(HandlerActions action, IEnumerable<uSyncAction> actions, string username);
}