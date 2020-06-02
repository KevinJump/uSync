using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Core;

namespace uSync8.BackOffice.Configuration
{
    /// <summary>
    ///  uSync Settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncSettings
    {
        /// <summary>
        ///  Location where all uSync files are saved by default
        /// </summary>
        public string RootFolder { get; set; } = "~/uSync/v8/";

        /// <summary>
        ///  Handlers store all files in one folder, no structure on the disk
        /// </summary>
        public bool UseFlatStructure { get; set; } = true;

        /// <summary>
        ///  File names are the guid key values of items.
        /// </summary>
        public bool UseGuidNames { get; set; } = false;

        /// <summary>
        ///  Items are saved in batches after being processed (as opposed to on at a time during import)
        /// </summary>
        public bool BatchSave { get; set; } = false;

        /// <summary>
        ///  Import when Umbraco boots
        /// </summary>
        public bool ImportAtStartup { get; set; } = false;

        /// <summary>
        ///  Export when Umbraco boots
        /// </summary>
        public bool ExportAtStartup { get; set; } = false;

        /// <summary>
        ///  Export when an item is saved in umbraco
        /// </summary>
        public bool ExportOnSave { get; set; } = true;

        /// <summary>
        ///  default handler set for processing events, exports etc.
        /// </summary>
        public string DefaultSet { get; set; } = "default";

        /// <summary>
        ///  collection of the handler sets loaded from the config 
        /// </summary>
        public IList<HandlerSet> HandlerSets { get; set; } = new List<HandlerSet>();
        //public List<HandlerSettings> Handlers { get; set; } = new List<HandlerSettings>();

        /// <summary>
        ///  Debug reports (creates an export into a temp folder for comparision)
        /// </summary>
        public bool ReportDebug { get; set; } = false;

        /// <summary>
        ///  Ping the AddOn Url to get the json to show in the addon dashboard
        /// </summary>
        public bool AddOnPing { get; set; } = true;

        /// <summary>
        /// pre Umbraco v8.4 - rebuild the cache was needed after content was imported.
        /// </summary>
        public bool RebuildCacheOnCompletion { get; set; } = false;

        /// <summary>
        ///  fail if the items parent is not in umbraco or part of the batch being imported
        /// </summary>
        public bool FailOnMissingParent { get; set; } = false;

        /// <summary>
        ///  Get the default handler set
        /// </summary>
        public HandlerSet DefaultHandlerSet()
            => this.HandlerSets.Where(x => x.Name.InvariantEquals(this.DefaultSet)).FirstOrDefault();
    }

    /// <summary>
    ///  A Handler set is a collection of handlers - defined in the config
    /// </summary>
    public class HandlerSet
    {
        /// <summary>
        ///  Name of the handler set
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  is this set enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///  List of handlers (and settings) from the set.
        /// </summary>
        public IList<HandlerSettings> Handlers { get; set; } = new List<HandlerSettings>();
    }

}
