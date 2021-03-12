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
        ///  The folder as stored in the settings file. 
        /// </summary>
        public string SettingsFolder { get; set; } = "~/uSync/v8/";

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
        ///  Import when Umbraco boots
        /// </summary>
        public bool ImportAtStartup { get; set; } = false;

        /// <summary>
        ///  The handler 'group' to use on startup import 
        /// </summary>
        public string ImportAtStartupGroup { get; set; } = string.Empty;

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
        ///  should we cache keys look ups etc, in the runtime cache ? most of the time 
        ///  the answer is yes - but for debugging we might want to turn this off. 
        /// </summary>
        public bool CacheFolderKeys { get; set; } = true;


        /// <summary>
        ///  Show a version check warning to the user if the folder version is less
        ///  than the version expected by uSync.
        /// </summary>
        public bool ShowVersionCheckWarning { get; set; } = false;

        /// <summary>
        ///  Get the default handler set
        /// </summary>
        /// <returns></returns>
        public HandlerSet DefaultHandlerSet()
            => this.HandlerSets.Where(x => x.Name.InvariantEquals(this.DefaultSet)).FirstOrDefault();

        /// <summary>
        ///  Custom mapping keys, allows users to add a simple config mapping
        ///  to make one property type to behave like an existing one
        /// </summary>
        public IDictionary<string, string> CustomMappings { get; set; }


        /// <summary>
        ///  options you can set at the top level, that are then set on all handlers.
        /// </summary>
        public IDictionary<string, string> DefaultHandlerSettings { get; set; }

        /// <summary>
        ///  location of SignalR hub script (/
        /// </summary>
        public string SignalRRoot { get; set; }
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
