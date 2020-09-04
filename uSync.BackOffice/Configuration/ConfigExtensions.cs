using Umbraco.Core.Configuration;

namespace uSync.BackOffice.Configuration
{
    public static class ConfigExtensions
    {
        /// <summary>
        ///  Get the uSync config from the Configs collection
        /// </summary>
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
