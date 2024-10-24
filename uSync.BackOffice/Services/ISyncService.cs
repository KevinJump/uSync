using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice;
public interface ISyncService
{
    /// <summary>
    ///  check the version file on disk, to see if we have older sync files.
    /// </summary>
    Task<bool> CheckVersionFileAsync(string folder);

    /// <summary>
    ///  check the version file(s) on the folders on disk.
    /// </summary>
    Task<bool> CheckVersionFileAsync(string[] folders);

    /// <summary>
    ///  clean the export folder (remove all files)
    /// </summary>
    bool CleanExportFolder(string folder);

    /// <summary>
    ///  compress (zip) up the folder -(so we can let people download it)
    /// </summary>
    MemoryStream CompressFolder(string folder);

    /// <summary>
    ///  decompress (unzip) an archive and put it on disk to be imported
    /// </summary>
    void DeCompressFile(string zipArchive, string target);  

    /// <summary>
    ///  run the export for a given folder
    /// </summary>
    Task<IEnumerable<uSyncAction>> ExportAsync(string folder, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks? callbacks);

    /// <summary>
    ///  export a handler to a folder based on the provided options
    /// </summary>
    Task<IEnumerable<uSyncAction>> ExportHandlerAsync(string handler, uSyncImportOptions options);
    
    /// <summary>
    ///  does the given folder contain any uSync files for content items? 
    /// </summary>
    bool HasContentFiles(string rootFolder);

    /// <summary>
    ///  do any of the given folders contain uSync files for content ? 
    /// </summary>
    /// <param name="folders"></param>
    /// <returns></returns>
    bool HasContentFiles(string[] folders);

    /// <summary>
    ///  checks if there are any files on disk for uSync at all. 
    /// </summary>
    /// <param name="folders"></param>
    /// <returns></returns>
    bool HasRootFiles(string[] folders);

    /// <summary>
    ///  import from the given folders 
    /// </summary>
    Task<IEnumerable<uSyncAction>> ImportAsync(string[] folders, bool force, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks? callbacks);

    /// <summary>
    ///  import for a given handler. 
    /// </summary>
    Task<IEnumerable<uSyncAction>> ImportHandlerAsync(string handlerAlias, uSyncImportOptions options);

    /// <summary>
    ///  import a partial set of items, based on the ordered nodes.
    /// </summary>
    Task<IEnumerable<uSyncAction>> ImportPartialAsync(IList<OrderedNodeInfo> orderedNodes, uSyncPagedImportOptions options);

    /// <summary>
    ///  run the post import tasks on a partial set of nodes
    /// </summary>
    Task<IEnumerable<uSyncAction>> ImportPartialPostImportAsync(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options);

    /// <summary>
    ///  run the second pass for a partial set of nodes
    /// </summary>
    Task<IEnumerable<uSyncAction>> ImportPartialSecondPassAsync(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options);

    /// <summary>
    ///  run the post import cleanup files.
    /// </summary>
    Task<IEnumerable<uSyncAction>> ImportPostCleanFilesAsync(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options);

    /// <summary>
    ///  import a single action from a given file 
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    Task<uSyncAction> ImportSingleActionAsync(uSyncAction action);

    /// <summary>
    ///  get the ordered nodes for a process and handler
    /// </summary>
    Task<IList<OrderedNodeInfo>> LoadOrderedNodesAsync(ISyncHandler handler, string[] handlerFolders);

    /// <summary>
    ///  perform the post import actions (happen at the very very very end).
    /// </summary>
    Task<IEnumerable<uSyncAction>> PerformPostImportAsync(string[] folders, string handlerSet, IEnumerable<uSyncAction> actions);

    /// <summary>
    ///  replace a file from the source with ones from the target, optionally cleaning the folder first.
    /// </summary>
    void ReplaceFiles(string source, string target, bool clean);

    /// <summary>
    ///  run a report for a given handler based on the folders in the options
    /// </summary> 
    Task<IEnumerable<uSyncAction>> ReportHandlerAsync(string handler, uSyncImportOptions options);

    /// <summary>
    ///  run a partial report for a given set of ordered nodes
    /// </summary>
    Task<IEnumerable<uSyncAction>> ReportPartialAsync(IList<OrderedNodeInfo> orderedNodes, uSyncPagedImportOptions options);

    Task<IEnumerable<uSyncAction>> StartupExportAsync(string folder, SyncHandlerOptions handlerOptions, uSyncCallbacks? callbacks = null);
    Task<IEnumerable<uSyncAction>> StartupImportAsync(string[] folders, bool force, SyncHandlerOptions handlerOptions, uSyncCallbacks? callbacks = null);

    /// <summary>
    ///  trigger the start of a bulk process
    /// </summary>
    Task StartBulkProcessAsync(HandlerActions action);

    /// <summary>
    ///  trigger the end of the bulk process
    /// </summary>
    Task FinishBulkProcessAsync(HandlerActions action, IEnumerable<uSyncAction> actions);

    // obsolete methods (all of these will go in v16
    //   implementations are mostly here in the interface, but there are a couple still in
    //   the service which need to be removed too. 

    [Obsolete("use ExportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> Export(string folder, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks? callbacks);

    [Obsolete("use ExportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> Export(string folder, IEnumerable<string> handlerAliases, uSyncCallbacks? callbacks);

    [Obsolete("use ExportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> ExportHandler(string handler, uSyncImportOptions options)
        => ExportHandlerAsync(handler, options).Result;

    [Obsolete("use ImportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> Import(string[] folders, bool force, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks? callbacks)
        => ImportAsync(folders, force, handlers, callbacks).Result;

    [Obsolete("use ImportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> ImportHandler(string handlerAlias, uSyncImportOptions options)
        => ImportHandlerAsync(handlerAlias, options).Result;

    [Obsolete("use ImportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> ImportPartial(IList<OrderedNodeInfo> orderedNodes, uSyncPagedImportOptions options, out int total)
    {
        total = orderedNodes.Count;
        return ImportPartialAsync(orderedNodes, options).Result;
    }
    [Obsolete("use ImportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> ImportPartialPostImport(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options)
        => ImportPartialPostImportAsync(actions, options).Result;

    [Obsolete("use ImportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> ImportPartialSecondPass(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options)
        => ImportPartialSecondPassAsync(actions, options).Result;

    [Obsolete("use ImportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> ImportPostCleanFiles(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options)
        => ImportPostCleanFilesAsync(actions, options).Result;

    [Obsolete("use ImportHandlerAsync will be removed in v16")]
    uSyncAction ImportSingleAction(uSyncAction action)
        => ImportSingleActionAsync(action).Result;

    [Obsolete("use ImportHandlerAsync will be removed in v16")]
    IList<OrderedNodeInfo> LoadOrderedNodes(ISyncHandler handler, string[] handlerFolders)
        => LoadOrderedNodesAsync(handler, handlerFolders).Result;

    [Obsolete("use ImportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> PerformPostImport(string[] folders, string handlerSet, IEnumerable<uSyncAction> actions)
        => PerformPostImportAsync(folders, handlerSet, actions).Result;

    [Obsolete("use ReportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> Report(string folder, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks? callbacks);
    [Obsolete("use ReportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> Report(string folder, IEnumerable<string> handlerAliases, uSyncCallbacks? callbacks);
    [Obsolete("use ReportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> Report(string folder, SyncHandlerOptions handlerOptions, uSyncCallbacks? callbacks = null);
   
    [Obsolete("use ReportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> ReportHandler(string handler, uSyncImportOptions options)
        => ReportHandlerAsync(handler, options).Result;

    [Obsolete("use ReportHandlerAsync will be removed in v16")]
    IEnumerable<uSyncAction> ReportPartial(IList<OrderedNodeInfo> orderedNodes, uSyncPagedImportOptions options, out int total)
    {
        total = orderedNodes.Count;
        return ReportPartialAsync(orderedNodes, options).Result;
    }

}