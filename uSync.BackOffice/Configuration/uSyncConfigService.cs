
using Microsoft.Extensions.Options;

namespace uSync.BackOffice.Configuration
{
    /// <summary>
    ///  manages the configuration settings for uSync, 
    /// </summary>
    public class uSyncConfigService
    {
        /// <summary>
        ///  uSync settings loaded from configuration
        /// </summary>
        public uSyncSettings Settings => _settingsMonitor.CurrentValue;

        private IOptionsMonitor<uSyncSettings> _settingsMonitor; 

        /// <summary>
        ///  The unmapped root folder for uSync.
        /// </summary>
        public string GetRootFolder()
            => Settings.RootFolder.TrimStart('/');

        public string GetBaseFolder()
            => Settings.BaseFolder.TrimStart('/');

        public string[] GetCombinedFolders()
            => new[]
            {
                GetRootFolder(),
                GetBaseFolder(),
            };

        private IOptionsMonitor<uSyncHandlerSetSettings> setOptionsMonitor;

        /// <summary>
        /// Constructor for config service
        /// </summary>
        public uSyncConfigService(
            IOptionsMonitor<uSyncSettings> settingsOptionsMonitor,
            IOptionsMonitor<uSyncHandlerSetSettings> setOptionsMonitor)
        {
            _settingsMonitor = settingsOptionsMonitor;
            this.setOptionsMonitor = setOptionsMonitor;
        }

        /// <summary>
        ///  get the settings for a named handler set.
        /// </summary>
        public uSyncHandlerSetSettings GetSetSettings(string setname)
            => setOptionsMonitor.Get(setname);

        /// <summary>
        ///  get the default handler settings for handlers
        /// </summary>
        public uSyncHandlerSetSettings GetDefaultSetSettings()
            => GetSetSettings(Settings.DefaultSet);

    }
}
