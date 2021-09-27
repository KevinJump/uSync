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

            public static string uSyncSetsConfigPrefix = uSyncConfigPrefix + "Sets:";
            public static string ConfigSettings = uSyncConfigPrefix + "Settings";

            // names option for the default set. 
            public static string ConfigDefaultSet = uSyncSetsConfigPrefix + uSync.Sets.DefaultSet;

        }
    }
}
