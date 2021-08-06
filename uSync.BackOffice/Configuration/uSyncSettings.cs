using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.Configuration
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncSettings
    {
        /// <summary>
        ///  Location where all uSync files are saved by default
        /// </summary>
        public string RootFolder { get; set; } = "uSync/v9/";

        public string DefaultSet { get; set; } = uSync.Sets.DefaultSet;

        /// <summary>
        ///  Import when Umbraco boots (can be group name or 'All' so everything is done, blank or 'none' == off)
        /// </summary>
        public string ImportAtStartup { get; set; } = "None";

        /// <summary>
        ///  Export when Umbraco boots
        /// </summary>
        public string ExportAtStartup { get; set; } = "None";

        /// <summary>
        ///  Export when an item is saved in Umbraco
        /// </summary>
        public string ExportOnSave { get; set; } = "All";


        /// <summary>
        ///  The handler groups that are enabled in the UI.
        /// </summary>
        public string UIEnabledGroups { get; set; } = "All";

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
