using System;
using System.Collections.Generic;

using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.SyncHandlers
{
    public delegate void SyncUpdateCallback(string message, int count, int total);

    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
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
        bool Enabled { get; set; }

        /// <summary>
        ///  default config for the handler - when being used in events.
        /// </summary>
        HandlerSettings DefaultConfig { get; set; }

        /// <summary>
        ///  Export all items 
        /// </summary>
        /// <param name="folder">folder to use when exporting</param>
        /// <param name="settings">Handler settings to use for export</param>
        /// <param name="callback">Callbacks to keep UI uptodate</param>
        /// <returns>List of actions detailing changes</returns>
        IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings settings, SyncUpdateCallback callback);

        /// <summary>
        ///  Import All items 
        /// </summary>
        /// <param name="folder">folder to use when Importing</param>
        /// <param name="settings">Handler settings to use for import</param>
        /// <param name="force">Force the import even if the settings haven't changed</param>
        /// <param name="callback">Callbacks to keep UI uptodate</param>
        /// <returns>List of actions detailing changes</returns>
        IEnumerable<uSyncAction> ImportAll(string folder, HandlerSettings settings, bool force, SyncUpdateCallback callback);

        /// <summary>
        ///  Report All items 
        /// </summary>
        /// <param name="folder">folder to use when reporting</param>
        /// <param name="settings">Handler settings to use for report</param>
        /// <param name="callback">Callbacks to keep UI uptodate</param>
        /// <returns>List of actions detailing changes</returns>
        IEnumerable<uSyncAction> Report(string folder, HandlerSettings settings, SyncUpdateCallback callback);

    }
}
