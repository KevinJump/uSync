
using Microsoft.Extensions.Options;

namespace uSync.BackOffice.Configuration
{
    /// <summary>
    ///  Manages the configuration settings for uSync, 
    /// </summary>
    public class uSyncConfigService
    {
        private IOptionsMonitor<uSyncHandlerSetSettings> _setOptionsMonitor;

        /// <summary>
        ///  uSync settings loaded from configuration
        /// </summary>
        public uSyncSettings Settings { get; set; }

        /// <summary>
        ///  The unmapped root folder for uSync.
        /// </summary>
        public string GetRootFolder()
            => Settings.RootFolder.TrimStart('/');


        /// <summary>
        /// Constructor for config service
        /// </summary>
        public uSyncConfigService(
            IOptionsMonitor<uSyncSettings> settingsOptionsMonitor,
            IOptionsMonitor<uSyncHandlerSetSettings> setOptionsMonitor)
        {
            Settings = settingsOptionsMonitor.CurrentValue;

            settingsOptionsMonitor.OnChange(options =>
            {
                Settings = options;
            });

            _setOptionsMonitor = setOptionsMonitor;

        }

        /// <summary>
        ///  get the settings for a named handler set.
        /// </summary>
        public uSyncHandlerSetSettings GetSetSettings(string setName)
            => _setOptionsMonitor.Get(setName);

        /// <summary>
        ///  get the default handler settings for handlers
        /// </summary>
        public uSyncHandlerSetSettings GetDefaultSetSettings()
            => GetSetSettings(Settings.DefaultSet);

    }
}
