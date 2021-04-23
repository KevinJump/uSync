
using Microsoft.Extensions.Options;

namespace uSync.BackOffice.Configuration
{
    /// <summary>
    ///  manipulation of the settings, so saving values back. 
    /// </summary>
    public class uSyncConfigService
    {
        public uSyncSettings Settings { get; private set; }

        private IOptionsMonitor<uSyncHandlerSetSettings> setOptionsMonitor;

        public uSyncConfigService(
            IOptions<uSyncSettings> options,
            IOptionsMonitor<uSyncHandlerSetSettings> setOptionsMonitor)
        {
            Settings = options.Value;
            this.setOptionsMonitor = setOptionsMonitor;
        }

        public uSyncHandlerSetSettings GetSetSettings(string setname)
            => setOptionsMonitor.Get(setname);

        public uSyncHandlerSetSettings GetDefaultSetSettings()
            => GetSetSettings(Settings.DefaultSet);

    }
}
