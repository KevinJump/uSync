
using Microsoft.Extensions.Options;

namespace uSync.BackOffice.Configuration
{
    /// <summary>
    ///  manipulation of the settings, so saving values back. 
    /// </summary>
    public class uSyncConfigService
    {
        public uSyncSettings Settings => _settingsMonitor.CurrentValue;

        private IOptionsMonitor<uSyncSettings> _settingsMonitor; 

        public string GetRootFolder()
            => Settings.RootFolder.TrimStart('/');

        private IOptionsMonitor<uSyncHandlerSetSettings> setOptionsMonitor;

        public uSyncConfigService(
            IOptionsMonitor<uSyncSettings> settingsOptionsMonitor,
            IOptionsMonitor<uSyncHandlerSetSettings> setOptionsMonitor)
        {
            _settingsMonitor = settingsOptionsMonitor;
            this.setOptionsMonitor = setOptionsMonitor;
        }

        public uSyncHandlerSetSettings GetSetSettings(string setname)
            => setOptionsMonitor.Get(setname);

        public uSyncHandlerSetSettings GetDefaultSetSettings()
            => GetSetSettings(Settings.DefaultSet);

    }
}
