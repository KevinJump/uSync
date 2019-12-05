using Umbraco.Core.Configuration;

namespace uSync8.BackOffice.Configuration
{
    public static class ConfigExtensions
    {
        public static uSyncSettings uSync(this Configs configs)
        {
            try
            {
                return configs.GetConfig<uSyncConfig>().Settings;
            }
            catch
            {
                return null;
            }
        }
            
    }
}
