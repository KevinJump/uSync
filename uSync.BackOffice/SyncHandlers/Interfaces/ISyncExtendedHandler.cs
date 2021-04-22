using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

using Umbraco.Cms.Core;

using uSync.BackOffice.Configuration;
using uSync.Core.Dependency;
using uSync.Core.Models;

namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    ///  A Extended Handler, lets you do things to just one item, 
    ///  like import/export it, or work out what dependencies it has. 
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public interface ISyncExtendedHandler : ISyncHandler
    {
        /// <summary>
        ///  Group handler belongs to (e.g Settings/Content/Users)
        /// </summary>
        string Group { get; }

        /// <summary>
        ///  Umbraco Entity Type handler deal in - (used when finding a sutible handler for the job)
        /// </summary>
        string EntityType { get; }

        /// <summary>
        ///  the Type name of the items the handler deals in.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        ///  Import a single item from a file
        /// </summary>
        /// <param name="file">uSync .config file of item you want to import</param>
        /// <param name="settings">Handler settings to use for process</param>
        /// <param name="force">force the import even if nothing has changed</param>
        /// <returns>List of actions that have happened as part of the process</returns>
        IEnumerable<uSyncAction> Import(string file, HandlerSettings settings, bool force);

        /// <summary>
        ///  Report on a single item from a file
        /// </summary>
        /// <param name="file">uSync .config file of item you want to report changes for</param>
        /// <param name="settings">Handler settings to use for process</param>
        /// <returns>List of actions that have happened as part of the process</returns>
        IEnumerable<uSyncAction> Report(string file, HandlerSettings settings);

        /// <summary>
        ///  Export a single item to disk
        /// </summary>
        /// <param name="id">Umbraco Id of the item you wish to export</param>
        /// <param name="folder">Location to save the .config file</param>
        /// <param name="settings">Handler settings to use for process</param>
        /// <returns>List of actions that have happened as part of the process</returns>
        IEnumerable<uSyncAction> Export(int id, string folder, HandlerSettings settings);

        /// <summary>
        ///  Export a single item to disk
        /// </summary>
        /// <param name="Udi">Umbraco udi of the item you wish to export</param>
        /// <param name="folder">Location to save the .config file</param>
        /// <param name="settings">Handler settings to use for process</param>
        /// <returns>List of actions that have happened as part of the process</returns>
        IEnumerable<uSyncAction> Export(Udi udi, string folder, HandlerSettings settings);

        /// <summary>
        ///  Get the XML representation of an item in umbraco
        /// </summary>
        /// <param name="udi">UDI of item</param>
        /// <returns>xml representation of an item</returns>
        SyncAttempt<XElement> GetElement(Udi udi);

        /// <summary>
        ///  Import a single element from XML
        /// </summary>
        /// <param name="element">XML of item to import</param>
        /// <param name="force">Import even if there is no changes in the file</param>
        /// <returns>List of actions that happen as part of process</returns>
        IEnumerable<uSyncAction> ImportElement(XElement element, bool force);

        /// <summary>
        ///  Report on single element from XML
        /// </summary>
        /// <param name="element">XML of item to report on</param>
        /// <returns>List of actions that happen as part of process</returns>
        IEnumerable<uSyncAction> ReportElement(XElement element);

        /// <summary>
        ///  Get a list of items that this item depends on
        /// </summary>
        /// <param name="id">Id of item to inspect</param>
        /// <param name="flags">flags detailing what to include in dependency list</param>
        /// <returns>List of items that main item is dependent upon</returns>
        IEnumerable<uSyncDependency> GetDependencies(int id, DependencyFlags flags);

        /// <summary>
        ///  Get a list of items that this item depends on
        /// </summary>
        /// <param name="key">Guid of item to inspect</param>
        /// <param name="flags">flags detailing what to include in dependency list</param>
        /// <returns>List of items that main item is dependent upon</returns>
        IEnumerable<uSyncDependency> GetDependencies(Guid key, DependencyFlags flags);
    }
}
