using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.Configuration
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncSettings
    {
        /// <summary>
        ///  Location where all uSync files are saved by default
        /// </summary>
        public string RootFolder { get; set; } = "/uSync/v9/";

        public string DefaultSet { get; set; } = uSync.Sets.DefaultSet;

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
        ///  Custom mapping keys, allows users to add a simple config mapping
        ///  to make one property type to behave like an existing one
        /// </summary>
        public IDictionary<string, string> CustomMappings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        ///  location of SignalR hub script (/
        /// </summary>
        public string SignalRRoot { get; set; } = string.Empty;

        /// <summary>
        ///  Should the history view be on of off ? 
        /// </summary>
        public bool EnableHistory { get; set; } = true;
   }

}
