using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.Extensions;

public static class HandlerSettingsExtensions
{
    public static bool IsCreateOnly(this HandlerSettings settings)
        => settings.GetSetting(Core.uSyncConstants.DefaultSettings.CreateOnly, Core.uSyncConstants.DefaultSettings.CreateOnly_Default)
            || settings.GetSetting(Core.uSyncConstants.DefaultSettings.OneWay, Core.uSyncConstants.DefaultSettings.CreateOnly_Default);

    public static bool AllowCreateOnlyDeletes(this HandlerSettings settings)
        => settings.GetSetting(Core.uSyncConstants.DefaultSettings.AllowCreateOnlyDeletes, Core.uSyncConstants.DefaultSettings.AllowCreateOnlyDeletes_Default);
}

