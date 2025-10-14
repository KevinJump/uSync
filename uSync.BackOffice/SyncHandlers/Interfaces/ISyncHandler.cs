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
using uSync.Core.Tracking;

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
    ///  get the serializer type for the handler (e.g the name used in the xml)
    /// </summary>
    string? GetSerializeType() => null;

    /// <summary>
    ///  gets the base tracker from the serializer (used to track changes, merge items).
    /// </summary>
    ISyncTrackerBase? GetBaseTracker() => null;

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
    ///  Async Export an item based on UDI
    /// </summary>
    Task<IEnumerable<uSyncAction>> ExportAsync(Udi udi, string[] folders, HandlerSettings settings);

    /// <summary>
    ///  async export all items in a folder
    /// </summary>
    Task<IEnumerable<uSyncAction>> ExportAllAsync(string[] folders, HandlerSettings settings, SyncUpdateCallback? callback);

    /// <summary>
    ///  Get the dependencies for an item
    /// </summary>
    Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(Guid key, DependencyFlags flags);

    /// <summary>
    ///  Get an XMLElement representation of an item by UDI
    /// </summary> 
    Task<SyncAttempt<XElement>> GetElementAsync(Udi udi);

    /// <summary>
    ///  Import an item from disk
    /// </summary> 
    Task<IEnumerable<uSyncAction>> ImportAsync(string file, HandlerSettings settings, bool force);

    /// <summary>
    ///  Import all items in a folder
    /// </summary>
    Task<IEnumerable<uSyncAction>> ImportAllAsync(string[] folders, HandlerSettings settings, uSyncImportOptions options);

    /// <summary>
    ///  import an XElement node
    /// </summary>
    Task<IEnumerable<uSyncAction>> ImportElementAsync(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options);

    /// <summary>
    ///  report all items in a folder
    /// </summary>
    Task<IEnumerable<uSyncAction>> ReportAsync(string[] folders, HandlerSettings settings, SyncUpdateCallback? callback);

    /// <summary>
    ///  Report an XELement node
    /// </summary>
    Task<IEnumerable<uSyncAction>> ReportElementAsync(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options);

    /// <summary>
    ///  Import the second pass of an item
    /// </summary>
    /// <param name="action"></param>
    /// <param name="settings"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<IEnumerable<uSyncAction>> ImportSecondPassAsync(uSyncAction action, HandlerSettings settings, uSyncImportOptions options);

    /// <summary>
    ///  find an item from a node
    /// </summary>
    Task<Udi?> FindFromNodeAsync(XElement node);

    /// <summary>
    ///  get the change status of an item compared to the XElement
    /// </summary>
    Task<ChangeType> GetItemStatusAsync(XElement node);

    /// <summary>
    ///  cache the folder keys for faster lookups
    /// </summary>
    Task PreCacheFolderKeysAsync(string folder, IList<Guid> keys);

    /// <summary>
    ///  get all items for the report/import process.
    /// </summary>
    /// <param name="folders"></param>
    /// <returns></returns>
    Task<IReadOnlyList<OrderedNodeInfo>> FetchAllNodesAsync(string[] folders);

    /// <summary>
    ///  find an element by its key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<XElement?> TryFindItemNodeAsync(Guid key) => Task.FromResult<XElement?>(null);
}
