
using Microsoft.Extensions.Options;

using System;
using System.Linq;

using Umbraco.Extensions;

namespace uSync.BackOffice.Configuration
{
    /// <summary>
    ///  Manages the configuration settings for uSync, 
    /// </summary>
    public class uSyncConfigService
    {
        private IOptionsMonitor<uSyncHandlerSetSettings> _setOptionsMonitor;
        private readonly SyncFolderCollection _syncFolders;

        /// <summary>
        /// Constructor for config service
        /// </summary>
        public uSyncConfigService(
            IOptionsMonitor<uSyncSettings> settingsOptionsMonitor,
            IOptionsMonitor<uSyncHandlerSetSettings> setOptionsMonitor,
            SyncFolderCollection syncFolders)
        {
            Settings = settingsOptionsMonitor.CurrentValue;

            settingsOptionsMonitor.OnChange(options =>
            {
                Settings = options;
            });

            _setOptionsMonitor = setOptionsMonitor;
            _syncFolders = syncFolders;
        }


        /// <summary>
        ///  uSync settings loaded from configuration
        /// </summary>
        public uSyncSettings Settings { get; set; }

        private string[] FetchFolders()
        {
            var folders = Settings.Folders
                .Select((x, index) => new SyncFolderItem
                {
                    Path = x.TrimStart('/').EnsureEndsWith('/'),
                    Weight = index * 1000
                })
                .ToList();

            folders.AddRange(
                _syncFolders.Select(x => new SyncFolderItem
                {
                    Path = x.Path.TrimStart('/').EnsureEndsWith('/'),
                    Weight = x.Weight
                })
                );

            return folders.OrderBy(x => x.Weight)
                .Select(x => x.Path)
                .Distinct()
                .ToArray();
        }

        private class SyncFolderItem
        {
            public required string Path { get; set; }
            public int Weight { get; set; }
        }


        /// <summary>
        ///  The unmapped root folder for uSync.
        /// </summary>
        [Obsolete("we should be using the array of folders, will be removed in v15")]
        public string GetRootFolder()
        {
            var folders = FetchFolders();

            return Settings.IsRootSite
                ? folders[0].TrimStart('/')
                : Settings.RootFolder.TrimStart('/');
        }

        /// <summary>
        ///  Get the root folders that uSync is using. 
        /// </summary>
        /// <returns></returns>
        public string[] GetFolders()
        {
            var folders = FetchFolders();

            return Settings.IsRootSite
                ? [folders[0].TrimStart('/')]
                : folders.Select(x => x.TrimStart('/')).ToArray();
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
