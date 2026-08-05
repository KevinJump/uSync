using System;

namespace uSync.BackOffice;

/// <summary>
///  global values for uSync 
/// </summary>
public class uSync
{
    /// <summary>
    ///  assembly version for uSync
    /// </summary>
    public static Version Version => typeof(uSync).Assembly.GetName().Version ?? new Version(15, 0, 0);

    /// <summary>
    ///  name of the app
    /// </summary>
    internal const string Name = "uSync";

    /// <summary>
    ///  a key we set on notifications, so you can tell if uSync processed them,
    /// </summary>
    public const string EventStateKey = "uSync.ProcessState";

    /// <summary>
    ///  a key set on a notification to say uSync was paused while processing the item.
    /// </summary>
    public const string EventPausedKey = "uSync.PausedKey";

    /// <summary>
    ///  a key we set on notifications, holding the keys of the items already exported
    ///  during the current Umbraco operation.
    /// </summary>
    /// <remarks>
    ///  a single operation can raise more than one notification for the same item (a
    ///  save-and-publish raises both the saved and the published notification), and they
    ///  all share one notification state, so we use it to only export the item once.
    /// </remarks>
    public const string EventExportedItemsKey = "uSync.ExportedItems";

    /// <summary>
    ///  the name we use internally for the 'everything' group.
    /// </summary>
    public const string EverythingGroupName = "All";

    internal class Trees
    {
        internal const string uSync = "usync";
        internal const string Group = "sync";
    }

    internal class Sets
    {
        internal const string DefaultSet = "Default";
    }

    /// <summary>
    ///  configuration defaults 
    /// </summary>
    public class Configuration
    {
        private const string uSyncConfigPrefix = "uSync:";

        /// <summary>
        ///  where the configuration settings live
        /// </summary>
        public static string ConfigSettings = uSyncConfigPrefix + "Settings";

        /// <summary>
        /// path to the root of the default configuration sets 
        /// </summary>
        public static string uSyncSetsConfig = uSyncConfigPrefix + "Sets";

        /// <summary>
        ///  prefix used for sets in the config 
        /// </summary>
        public static string uSyncSetsConfigPrefix = uSyncSetsConfig + ":";

        /// <summary>
        /// names option for the default set. 
        /// </summary>
        public static string ConfigDefaultSet = uSyncSetsConfigPrefix + uSync.Sets.DefaultSet;

    }
}
