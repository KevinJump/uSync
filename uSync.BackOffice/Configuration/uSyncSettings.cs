using System;
using System.Collections.Generic;
using System.ComponentModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.Configuration
{
    /// <summary>
    /// uSync Settings
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncSettings
    {
        /// <summary>
        /// Location where all uSync files are saved by default
        /// </summary>
        [DefaultValue("uSync/v9/")]
        public string RootFolder { get; set; } = "uSync/v9/";

        /// <summary>
        ///  collection of folders uSync looks in when performing imports.
        /// </summary>
        [DefaultValue("uSync/Root/, uSync/v9")]
        public string[] Folders { get; set; } = [];

        /// <summary>
        ///  Sets this site to be the root site (so it will save into "uSync/root/")
        /// </summary>
        [DefaultValue(false)]
        public bool IsRootSite { get; set; } = false;

        /// <summary>
        /// when locked you can't make changes to anything that is in the root
        /// </summary>
        public bool LockRoot { get; set; } = true;

        /// <summary>
        ///  lock specific types at root so they can't be changed in child sites. 
        /// </summary>
        /// <remarks>
        ///  document, media, member, dictionary-item, macro, template, document-type, 
        ///  media-type, data-type, member-type, member-group, relation-type, forms-form,
        ///  forms-prevalue, forms-datasource, language
        /// </remarks>
        public string[] LockRootTypes { get; set; } = [];

        /// <summary>
        /// The default handler set to use on all notification triggered events
        /// </summary>
        [DefaultValue(uSync.Sets.DefaultSet)]
        public string DefaultSet { get; set; } = uSync.Sets.DefaultSet;

        /// <summary>
        /// Import when Umbraco boots (can be group name or 'All' so everything is done, blank or 'none' == off)
        /// </summary>
        [DefaultValue("None")]
        public string ImportAtStartup { get; set; } = "None";

        /// <summary>
        /// Export when Umbraco boots
        /// </summary>
        [DefaultValue("None")]
        public string ExportAtStartup { get; set; } = "None";

        /// <summary>
        /// Export when an item is saved in Umbraco
        /// </summary>
        [DefaultValue("All")]
        public string ExportOnSave { get; set; } = "All";


        /// <summary>
        /// The handler groups that are enabled in the UI.
        /// </summary>
        [DefaultValue("All")]
        public string UIEnabledGroups { get; set; } = "All";

        /// <summary>
        /// Debug reports (creates an export into a temp folder for comparison)
        /// </summary>
        [DefaultValue(false)]
        public bool ReportDebug { get; set; } = false;

        /// <summary>
        /// Ping the AddOnUrl to get the json used to show the addons dashboard
        /// </summary>
        [DefaultValue(true)]
        public bool AddOnPing { get; set; } = true;

        /// <summary>
        /// Pre Umbraco 8.4 - rebuild the cache was needed after content was imported
        /// </summary>
        [DefaultValue(false)]
        public bool RebuildCacheOnCompletion { get; set; } = false;

        /// <summary>
        /// Fail if the items parent is not in umbraco or part of the batch being imported
        /// </summary>
        [DefaultValue(false)]
        public bool FailOnMissingParent { get; set; } = false;

        /// <summary>
        ///  fail if a duplicate file of same type and key is detected during the import process.
        /// </summary>
        [DefaultValue(false)]
        public bool FailOnDuplicates { get; set; } = false;

        /// <summary>
        /// Should folder keys be cached (for speed)
        /// </summary>
        [DefaultValue(true)]
        public bool CacheFolderKeys { get; set; } = true;


        /// <summary>
        /// Show a version check warning to the user if the folder version is less than the version expected by uSync.
        /// </summary>
        [DefaultValue(true)]
        public bool ShowVersionCheckWarning { get; set; } = true;

        /// <summary>
        /// Custom mapping keys, allows users to add a simple config mapping to make one property type to behave like an existing one
        /// </summary>
        public IDictionary<string, string> CustomMappings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// location of SignalR hub script
        /// </summary>
        [DefaultValue("")]
        [Obsolete("Will remove the option to move the route in v13")]
        public string SignalRRoot { get; set; } = string.Empty;

        /// <summary>
        /// Should the history view be on of off ? 
        /// </summary>
        [DefaultValue(true)]
        public bool EnableHistory { get; set; } = true;

        /// <summary>
        /// Location of the history folder.
        /// </summary>
        public string HistoryFolder { get; set; } = string.Empty;

        /// <summary>
        /// Default file extension for the uSync files. 
        /// </summary>
        [DefaultValue("config")]
        public string DefaultExtension { get; set; } = "config";

        /// <summary>
        /// Import the uSync folder on the first boot. 
        /// </summary>
        [DefaultValue(false)]
        public bool ImportOnFirstBoot { get; set; } = false;

        /// <summary>
        /// Handler group(s) to run on first boot, default is All (so full import)
        /// </summary>
        [DefaultValue("All")]
        public string FirstBootGroup { get; set; } = "All";

        /// <summary>
        /// Disable the default dashboard (so people can't accedently press the buttons).
        /// </summary>
        [DefaultValue("false")]
        public bool DisableDashboard { get; set; } = false;

        /// <summary>
        ///  summerize results (for when there are loads and loads of items)
        /// </summary>
        [DefaultValue("false")]
        public bool SummaryDashboard { get; set; } = false;

        /// <summary>
        ///  limit of items to display before flicking to summary view. (this is per handler)
        /// </summary>
        [DefaultValue(1000)]
        public int SummaryLimit { get; set; } = 1000;

        /// <summary>
        ///  list of addon (tabs) you don't want to show inside uSync dashboard.
        /// </summary>
        public string HideAddOns { get; set; } = "licence";

        /// <summary>
        ///  turns of use of the Notifications.Supress method, so notifications
        ///  are suppressed during the import
        /// </summary>
        /// <remarks>
        ///  this will result in no notifications being fired througout the import,
        ///  use this if you are comfortable with the risks
        /// </remarks>
        [DefaultValue("false")]
        public bool EnableNotificationSuppression { get; set; } = false;

        /// <summary>
        ///  trigger all the notifications in a background thread, 
        /// </summary>
        /// <remarks>
        ///  uSync will process imports faster, but any notification events will
        ///  fire off afterward in the background.
        /// </remarks>
        [DefaultValue(false)]
        public bool BackgroundNotifications { get; set; } = false;
    }
}
