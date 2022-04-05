namespace uSync.BackOffice
{
    public class uSync
    {
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

        public class Configuration
        {
            private const string uSyncConfigPrefix = "uSync:";
            public static string ConfigSettings = uSyncConfigPrefix + "Settings";

            public static string uSyncSetsConfig = uSyncConfigPrefix + "Sets";
            public static string uSyncSetsConfigPrefix = uSyncSetsConfig + ":";

            // names option for the default set. 
            public static string ConfigDefaultSet = uSyncSetsConfigPrefix + uSync.Sets.DefaultSet;

        }
    }
}
