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
    Task<bool> CheckVersionFileAsync(string folder);
    Task<bool> CheckVersionFileAsync(string[] folders);
    bool CleanExportFolder(string folder);
    MemoryStream CompressFolder(string folder);
    void DeCompressFile(string zipArchive, string target);  
    Task<IEnumerable<uSyncAction>> ExportAsync(string folder, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks? callbacks);
    Task<IEnumerable<uSyncAction>> ExportHandlerAsync(string handler, uSyncImportOptions options);
    Task FinishBulkProcessAsync(HandlerActions action, IEnumerable<uSyncAction> actions);
    bool HasContentFiles(string rootFolder);
    bool HasContentFiles(string[] folders);
    bool HasRootFiles(string[] folders);

    Task<IEnumerable<uSyncAction>> ImportAsync(string[] folders, bool force, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks? callbacks);
    Task<IEnumerable<uSyncAction>> ImportHandlerAsync(string handlerAlias, uSyncImportOptions options);
    Task<IEnumerable<uSyncAction>> ImportPartialAsync(IList<OrderedNodeInfo> orderedNodes, uSyncPagedImportOptions options);
    Task<IEnumerable<uSyncAction>> ImportPartialPostImportAsync(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options);
    Task<IEnumerable<uSyncAction>> ImportPartialSecondPassAsync(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options);
    Task<IEnumerable<uSyncAction>> ImportPostCleanFilesAsync(IEnumerable<uSyncAction> actions, uSyncPagedImportOptions options);
    Task<uSyncAction> ImportSingleActionAsync(uSyncAction action);
    Task<IList<OrderedNodeInfo>> LoadOrderedNodesAsync(ISyncHandler handler, string[] handlerFolders);
    Task<IEnumerable<uSyncAction>> PerformPostImportAsync(string[] folders, string handlerSet, IEnumerable<uSyncAction> actions);
    void ReplaceFiles(string source, string target, bool clean);
   
    Task<IEnumerable<uSyncAction>> ReportHandlerAsync(string handler, uSyncImportOptions options);

    Task<IEnumerable<uSyncAction>> ReportPartialAsync(IList<OrderedNodeInfo> orderedNodes, uSyncPagedImportOptions options);
    Task StartBulkProcessAsync(HandlerActions action);
    Task<IEnumerable<uSyncAction>> StartupExportAsync(string folder, SyncHandlerOptions handlerOptions, uSyncCallbacks? callbacks = null);
    Task<IEnumerable<uSyncAction>> StartupImportAsync(string[] folders, bool force, SyncHandlerOptions handlerOptions, uSyncCallbacks? callbacks = null);


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