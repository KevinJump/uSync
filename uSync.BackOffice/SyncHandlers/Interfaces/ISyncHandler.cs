using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Cms.Core;

using uSync.BackOffice.Configuration;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Models;

namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    ///  callback delegate for SignalR messaging 
    /// </summary>
    public delegate void SyncUpdateCallback(string message, int count, int total);

    /// <summary>
    ///  Handler interface for anything that wants to process elements via uSync
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
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
        /// Umbraco entity type manged by the handler 
        /// </summary>
        string EntityType { get; }

        /// <summary>
        /// The type name of the items handled (Item.getType().ToString())
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Export an item based on the int id value in umbraco
        /// </summary>
        IEnumerable<uSyncAction> Export(int id, string folder, HandlerSettings settings);

        /// <summary>
        /// Export an item based on the Udi value of the item
        /// </summary>
        IEnumerable<uSyncAction> Export(Udi udi, string folder, HandlerSettings settings);

        /// <summary>
        ///  Export all items 
        /// </summary>
        /// <param name="folder">folder to use when exporting</param>
        /// <param name="settings">Handler settings to use for export</param>
        /// <param name="callback">Callbacks to keep UI upto date</param>
        /// <returns>List of actions detailing changes</returns>
        IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings settings, SyncUpdateCallback callback);

        /// <summary>
        /// Get any dependencies required to full import this item
        /// </summary>
        IEnumerable<uSyncDependency> GetDependencies(int id, DependencyFlags flags);
        /// <summary>
        /// Get any dependencies required to full import this item
        /// </summary>
        IEnumerable<uSyncDependency> GetDependencies(Guid key, DependencyFlags flags);

        /// <summary>
        /// Get an XML representation of an item based on its UDI value 
        /// </summary>
        SyncAttempt<XElement> GetElement(Udi udi);

        /// <summary>
        /// Import an item from disk defined by the file name 
        /// </summary>
        IEnumerable<uSyncAction> Import(string file, HandlerSettings settings, bool force);

        /// <summary>
        ///  Import All items 
        /// </summary>
        /// <param name="folder">folder to use when Importing</param>
        /// <param name="settings">Handler settings to use for import</param>
        /// <param name="force">Force the import even if the settings haven't changed</param>
        /// <param name="callback">Callbacks to keep UI upto date</param>
        /// <returns>List of actions detailing changes</returns>
        IEnumerable<uSyncAction> ImportAll(string folder, HandlerSettings settings, bool force, SyncUpdateCallback callback);

        /// <summary>
        ///  Import from a single node. 
        /// </summary>
        IEnumerable<uSyncAction> ImportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options);

        /// <summary>
        ///  Report All items 
        /// </summary>
        /// <param name="folder">folder to use when reporting</param>
        /// <param name="settings">Handler settings to use for report</param>
        /// <param name="callback">Callbacks to keep UI upto date</param>
        /// <returns>List of actions detailing changes</returns>
        IEnumerable<uSyncAction> Report(string folder, HandlerSettings settings, SyncUpdateCallback callback);

        /// <summary>
        /// Report a single item based on loaded uSync xml
        /// </summary>
        IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options);


        /// <summary>
        ///  Import the second pass of an item.
        /// </summary>
        IEnumerable<uSyncAction> ImportSecondPass(uSyncAction action, HandlerSettings settings, uSyncImportOptions options);

        /// <summary>
        ///  default implementation, root handler does do this. 
        /// </summary>
        Udi FindFromNode(XElement node) => null;

        /// <summary>
        ///  is this a current node (root handler can do this too)
        /// </summary>
        ChangeType GetItemStatus(XElement node) => ChangeType.NoChange;


        /// <summary>
        ///  precaches the keys of a folder
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="keys"></param>
        void PreCacheFolderKeys(string folder, IList<Guid> keys) { }
    }
}
