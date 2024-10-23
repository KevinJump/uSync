using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Cms.Core;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Models;

namespace uSync.BackOffice.SyncHandlers.Interfaces;

/// <summary>
///  callback delegate for SignalR messaging 
/// </summary>
public delegate void SyncUpdateCallback(string message, int count, int total);

/// <summary>
///  Handler interface for anything that wants to process elements via uSync
/// </summary>
public interface ISyncHandler
{
    /// <summary>
    ///  alias for handler, used when finding a handler 
    /// </summary>
    string Alias { get; }

    /// <summary>
    ///  display name for handler
    /// </summary>
    string Name { get; }

    /// <summary>
    ///  priority order for handler
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///  default folder name for handler
    /// </summary>
    string DefaultFolder { get; }

    /// <summary>
    ///  Icon to use in the UI when this handler is displayed
    /// </summary>
    string Icon { get; }

    /// <summary>
    ///  type of model handler works with
    /// </summary>
    string ItemType { get; }

    /// <summary>
    ///  is the handler enabled.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    ///  default config for the handler - when being used in events.
    /// </summary>
    HandlerSettings DefaultConfig { get; set; }

    /// <summary>
    /// Group handler belongs too
    /// </summary>
    string Group { get; }

    /// <summary>
    /// Umbraco entity type managed by the handler 
    /// </summary>
    string EntityType { get; }

    /// <summary>
    /// The type name of the items handled (Item.getType().ToString())
    /// </summary>
    string TypeName { get; }

    /// <summary>
    /// Export an item based on the int id value in umbraco
    /// </summary>
    /// <remarks>
    /// these export methods do not obey roots, there are for use
    /// only when exporting to a custom folder.
    /// </remarks>
    [Obsolete("Export by Udi will be removed in v16")]
    IEnumerable<uSyncAction> Export(int id, string folder, HandlerSettings settings);

    /// <summary>
    /// Export an item based on the Udi value of the item
    /// </summary>
    /// <remarks>
    /// these export methods do not obey roots, there are for use
    /// only when exporting to a custom folder.
    /// </remarks>
    [Obsolete("Export by passing in folder array for root support - will be removed in v16")]
    IEnumerable<uSyncAction> Export(Udi udi, string folder, HandlerSettings settings)
        => ExportAsync(udi, new[] { folder }, settings).Result;

    [Obsolete("use ExportAsync will be removed in v16")]
    IEnumerable<uSyncAction> Export(Udi udi, string[] folders, HandlerSettings settings)
        => ExportAsync(udi, folders, settings).Result;

    /// <summary>
    ///  Export all items 
    /// </summary>
    /// <param name="folders">folders to use when exporting</param>
    /// <param name="settings">Handler settings to use for export</param>
    /// <param name="callback">Callbacks to keep UI up to date</param>
    /// <returns>List of actions detailing changes</returns>
    [Obsolete("use ExportAllAsync will be removed in v16")]
    IEnumerable<uSyncAction> ExportAll(string[] folders, HandlerSettings settings, SyncUpdateCallback? callback);

    /// <summary>
    /// Get any dependencies required to full import this item
    /// </summary>
    [Obsolete("use GetDependenciesAsync will be removed in v16")]
    IEnumerable<uSyncDependency> GetDependencies(int id, DependencyFlags flags) => [];

    /// <summary>
    /// Get any dependencies required to full import this item
    /// </summary>
    [Obsolete("use GetDependenciesAsync will be removed in v16")]
    IEnumerable<uSyncDependency> GetDependencies(Guid key, DependencyFlags flags)
        => GetDependenciesAsync(key, flags).Result;


    /// <summary>
    /// Get an XML representation of an item based on its UDI value 
    /// </summary>
    [Obsolete("use GetElementAsync will be removed in v16")]
    SyncAttempt<XElement> GetElement(Udi udi)
        => GetElementAsync(udi).Result;


    /// <summary>
    /// Import an item from disk defined by the file name 
    /// </summary>
    [Obsolete("use ImportAsync will be removed in v16")]
    IEnumerable<uSyncAction> Import(string file, HandlerSettings settings, bool force)
        => ImportAsync(file, settings, force).Result;

    /// <summary>
    ///  Import All items 
    /// </summary>
    /// <param name="folders">folders to use when Importing</param>
    /// <param name="settings">Handler settings to use for import</param>
    /// <param name="options">Import options to use</param>
    /// <returns>List of actions detailing changes</returns>
    [Obsolete("use ImportAllAsync will be removed in v16")]
    IEnumerable<uSyncAction> ImportAll(string[] folders, HandlerSettings settings, uSyncImportOptions options)
        => ImportAllAsync(folders, settings, options).Result;

    /// <summary>
    ///  Import from a single node. 
    /// </summary>
    [Obsolete("use ImportElementAsync will be removed in v16")]
    IEnumerable<uSyncAction> ImportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options)
        => ImportElementAsync(node, filename, settings, options).Result;

    /// <summary>
    ///  Report All items 
    /// </summary>
    /// <param name="folders">folders to use when reporting</param>
    /// <param name="settings">Handler settings to use for report</param>
    /// <param name="callback">Callbacks to keep UI up to date</param>
    /// <returns>List of actions detailing changes</returns>
    [Obsolete("use ReportAsync will be removed in v16")]
    IEnumerable<uSyncAction> Report(string[] folders, HandlerSettings settings, SyncUpdateCallback? callback)
        => ReportAsync(folders, settings, callback).Result;

    /// <summary>
    /// Report a single item based on loaded uSync xml
    /// </summary>
    [Obsolete("use ReportElementAsync will be removed in v16")]
    IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options)
        => ReportElementAsync(node, filename, settings, options).Result;

    /// <summary>
    ///  Import the second pass of an item.
    /// </summary>
    [Obsolete("use ImportSecondPassAsync will be removed in v16")]
    IEnumerable<uSyncAction> ImportSecondPass(uSyncAction action, HandlerSettings settings, uSyncImportOptions options)
        => ImportSecondPassAsync(action, settings, options).Result;

    /// <summary>
    ///  default implementation, root handler does do this. 
    /// </summary>
    [Obsolete("use FindFromNodeAsync will be removed in v16")]
    Udi? FindFromNode(XElement node) 
        => FindFromNodeAsync(node).Result;

    /// <summary>
    ///  is this a current node (root handler can do this too)
    /// </summary>
    [Obsolete("use GetItemStatusAsync will be removed in v16")]
    ChangeType GetItemStatus(XElement node);

    /// <summary>
    ///  precaches the keys of a folder
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="keys"></param>
    [Obsolete("use PreCacheFolderKeysAsync will be removed in v16")]
    void PreCacheFolderKeys(string folder, IList<Guid> keys)
        => PreCacheFolderKeysAsync(folder, keys).Wait();


    /// <summary>
    ///  fetch all the nodes that are needed for an report/import.
    /// </summary>
    [Obsolete("use FetchAllNodesAsync will be removed in v16")]
    public IReadOnlyList<OrderedNodeInfo> FetchAllNodes(string[] folders)
        => FetchAllNodesAsync(folders).Result;


    // async all the things... 

    Task<IEnumerable<uSyncAction>> ExportAsync(Udi udi, string[] folders, HandlerSettings settings);   
    Task<IEnumerable<uSyncAction>> ExportAllAsync(string[] folders, HandlerSettings settings, SyncUpdateCallback? callback);
    Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(Guid key, DependencyFlags flags);
    Task<SyncAttempt<XElement>> GetElementAsync(Udi udi);
    Task<IEnumerable<uSyncAction>> ImportAsync(string file, HandlerSettings settings, bool force);
    Task<IEnumerable<uSyncAction>> ImportAllAsync(string[] folders, HandlerSettings settings, uSyncImportOptions options);
    Task<IEnumerable<uSyncAction>> ImportElementAsync(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options);
    Task<IEnumerable<uSyncAction>> ReportAsync(string[] folders, HandlerSettings settings, SyncUpdateCallback? callback);
    Task<IEnumerable<uSyncAction>> ReportElementAsync(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options);
    Task<IEnumerable<uSyncAction>> ImportSecondPassAsync(uSyncAction action, HandlerSettings settings, uSyncImportOptions options);

    Task<Udi?> FindFromNodeAsync(XElement node);
    Task<ChangeType> GetItemStatusAsync(XElement node);
    Task PreCacheFolderKeysAsync(string folder, IList<Guid> keys);
    Task<IReadOnlyList<OrderedNodeInfo>> FetchAllNodesAsync(string[] folders);
}
