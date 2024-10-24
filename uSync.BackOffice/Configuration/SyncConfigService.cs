
using System;
using System.Linq;

using Microsoft.Extensions.Options;

namespace uSync.BackOffice.Configuration;

/// <inheritdoc/>
internal class SyncConfigService : ISyncConfigService
{
    private IOptionsMonitor<uSyncHandlerSetSettings> _setOptionsMonitor;

    /// <inheritdoc/>
    public uSyncSettings Settings { get; private set; }

    /// <summary>
    /// Constructor for config service
    /// </summary>
    public SyncConfigService(
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

    /// <inheritdoc/>
    public string GetWorkingFolder()
        => Settings.IsRootSite
            ? Settings.Folders[0].TrimStart('/')
            : Settings.Folders.Last().TrimStart('/');

    /// <inheritdoc/>
    public string[] GetFolders()
        => Settings.IsRootSite
            ? [Settings.Folders[0].TrimStart('/')]
            : Settings.Folders.Select(x => x.TrimStart('/')).ToArray();

    /// <inheritdoc/>
    public uSyncHandlerSetSettings GetSetSettings(string setName)
        => _setOptionsMonitor.Get(setName);

    /// <inheritdoc/>
    public uSyncHandlerSetSettings GetDefaultSetSettings()
        => GetSetSettings(Settings.DefaultSet);
}
