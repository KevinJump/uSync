namespace uSync.BackOffice.Configuration;

/// <summary>
///  configuration settings for uSync 
/// </summary>
public interface ISyncConfigService
{
    /// <summary>
    ///  the settings loaded from configuration
    /// </summary>
    uSyncSettings Settings { get; }

    /// <summary>
    ///  get the list of folders uSync is looking at 
    /// </summary>
    string[] GetFolders();

    /// <summary>
    ///  get the list of folders uSync looks at for a named set
    /// </summary>
    /// <remarks>
    ///  returns the set's own folders if it has any configured, otherwise the global folders.
    ///  a null or empty set name means the default set.
    /// </remarks>
    string[] GetFolders(string? setName) => GetFolders();

    /// <summary>
    ///  the handler set used by the startup import, startup export and first boot import.
    /// </summary>
    /// <remarks>
    ///  the StartupSet setting, or the Default set when that is blank (what startup has always used).
    /// </remarks>
    string GetStartupSet()
        => string.IsNullOrWhiteSpace(Settings.StartupSet) ? uSync.Sets.DefaultSet : Settings.StartupSet;

    /// <summary>
    ///  get the settings for the default set
    /// </summary>
    uSyncHandlerSetSettings GetDefaultSetSettings();

    /// <summary>
    ///  get the settings for a named set
    /// </summary>
    uSyncHandlerSetSettings GetSetSettings(string setName);

    /// <summary>
    ///  get the working folder for core operations (e.g startup, exports, locks etc)
    /// </summary>
    string GetWorkingFolder();
}