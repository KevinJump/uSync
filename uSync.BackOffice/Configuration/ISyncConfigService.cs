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