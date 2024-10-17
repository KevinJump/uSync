
using System;
using System.Linq;

using Microsoft.Extensions.Options;

namespace uSync.BackOffice.Configuration;

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
    ///  the folder that startup operations will look in (export, locks etc).
    /// </summary>
    public string GetWorkingFolder()
        => Settings.IsRootSite 
            ? Settings.Folders[0].TrimStart('/')
            : Settings.Folders.Last().TrimStart('/');

    /// <summary>
    ///  Get the root folders that uSync is using. 
    /// </summary>
    /// <returns></returns>
    public string[] GetFolders()
        => Settings.IsRootSite
            ? [Settings.Folders[0].TrimStart('/')]
            : Settings.Folders.Select(x => x.TrimStart('/')).ToArray();


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
