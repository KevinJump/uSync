using System.ComponentModel;

using uSync.BackOffice.Configuration;

namespace uSync;

internal class AppSettings
{
    public uSyncDefinition uSync { get; set; }

    /// <summary>
    /// Configuration of uSync settings
    /// </summary>
    internal class uSyncDefinition
    {
        /// <summary>
        /// uSync settings
        /// </summary>
        public uSyncSettings Settings { get; set; }

        /// <summary>
        /// Force uSync to use FIPS compliant hashing algorthims when comparing files
        /// </summary>
        public bool ForceFips { get; set; }

        /// <summary>
        /// Settings of Handler sets
        /// </summary>
        public uSyncSetsDefinition Sets { get; set; }

        /// <summary>
        /// Settings for the AutoTemplates package, (dynamic adding of templates based on files on disk)
        /// </summary>
        public AutoTemplatesDefinition AutoTemplates { get; set; }

        internal class uSyncSetsDefinition
        {
            public uSyncHandlerSetSettings Default { get; set; }
        }

        internal class AutoTemplatesDefinition
        {
            /// <summary>
            /// Enable AutoTemplates feature
            /// </summary>
            [DefaultValue(false)]
            public bool Enabled { get; set; }

            /// <summary>
            /// Delete templates from Umbraco if the file is missing from disk
            /// </summary>
            [DefaultValue(false)]
            public bool Delete { get; set; }

            /// <summary>
            /// Amount of time (milliseconds) to wait after file change event before applying changes
            /// </summary>
            [DefaultValue(1000)]
            public int Delay { get; set; }

        }
    }
}
