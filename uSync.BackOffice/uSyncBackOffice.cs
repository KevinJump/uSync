namespace uSync.BackOffice
{
    /// <summary>
    ///  global values for uSync 
    /// </summary>
    public class uSync
    {
        /// <summary>
        ///  name of the app
        /// </summary>
        internal const string Name = "uSync";

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
}
