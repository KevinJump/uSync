using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Core;

namespace uSync8.BackOffice.Configuration
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncSettings
    {
        /// <summary>
        ///  Location where all uSync files are saved by default
        /// </summary>
        public string RootFolder { get; set; } = "~/uSync/v8/";

        /// <summary>
        ///  Handlers store all files in one folder, no structure on disk
        /// </summary>
        public bool UseFlatStructure { get; set; } = true;

        /// <summary>
        ///  file names are the guid key values of items.
        /// </summary>
        public bool UseGuidNames { get; set; } = false;

        /// <summary>
        ///  items are saved in batches after being process (as opposed to one at a time during import)
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
        ///  Export when an item is saved in Umbraco
        /// </summary>
        public bool ExportOnSave { get; set; } = true;

        /// <summary>
        ///  default handler set for preocessing events, exports, etc
        /// </summary>
        public string DefaultSet { get; set; } = "default";

        /// <summary>
        ///  collection of the handler sets loaded from the config file.
        /// </summary>
        public IList<HandlerSet> HandlerSets { get; set; } = new List<HandlerSet>();

        /// <summary>
        ///  Debug reports (creates an export into a temp folder for comparison)
        /// </summary>
        public bool ReportDebug { get; set; } = false;

        /// <summary>
        ///  Ping the AddOnUrl to get the json used to show the addons dashboard
        /// </summary>
        public bool AddOnPing { get; set; } = true;

        /// <summary>
        ///  pre Umbraco 8.4 - rebuild the cache was needed after content was imported
        /// </summary>
        public bool RebuildCacheOnCompletion { get; set; } = false;

        /// <summary>
        ///  fail if the items parent is not in umbraco or part of the batch being imported
        /// </summary>
        public bool FailOnMissingParent { get; set; } = false;


        /// <summary>
        ///  Get the default handler set
        /// </summary>
        /// <returns></returns>
        public HandlerSet DefaultHandlerSet()
            => this.HandlerSets.Where(x => x.Name.InvariantEquals(this.DefaultSet)).FirstOrDefault();
    }

    public class HandlerSet
    {
        /// <summary>
        ///  Name of the handler set
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  Is this set enabled ?
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///  List of handlers (and settings) from the set.
        /// </summary>
        public IList<HandlerSettings> Handlers { get; set; } = new List<HandlerSettings>();
    }

}
