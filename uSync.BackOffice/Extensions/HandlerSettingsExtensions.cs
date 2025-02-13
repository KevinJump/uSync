using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.Extensions;

/// <summary>
///  helpers for the config of handlers.
/// </summary>
public static class HandlerSettingsExtensions
{
    /// <summary>
    ///  is the handler setup for creation of new items only ?
    /// </summary>
    public static bool IsCreateOnly(this HandlerSettings settings)
        => settings.GetSetting(Core.uSyncConstants.DefaultSettings.CreateOnly, Core.uSyncConstants.DefaultSettings.CreateOnly_Default)
            || settings.GetSetting(Core.uSyncConstants.DefaultSettings.OneWay, Core.uSyncConstants.DefaultSettings.CreateOnly_Default);

    /// <summary>
    ///  are deletes allowed with the creation of new items. 
    /// </summary>
    public static bool AllowCreateOnlyDeletes(this HandlerSettings settings)
        => settings.GetSetting(Core.uSyncConstants.DefaultSettings.AllowCreateOnlyDeletes, Core.uSyncConstants.DefaultSettings.AllowCreateOnlyDeletes_Default);
}

