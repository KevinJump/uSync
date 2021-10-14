using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using System;
using System.IO;

namespace uSync
{
    public static class uSyncConfigExtensions
    {
        public static void AdduSyncConfigs(this IConfigurationBuilder configurationBuilder, HostBuilderContext builderContext, string filename)
        {
            var env = builderContext.HostingEnvironment;
            var fileRootName = 
                Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));

            configurationBuilder.AddJsonFile($"{fileRootName}.json", optional: true, reloadOnChange: true);
            configurationBuilder.AddJsonFile($"{fileRootName}.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
            configurationBuilder.AddJsonFile($"{fileRootName}.{Environment.MachineName}.json", optional: true, reloadOnChange: true);
        }

        public static IHostBuilder ConfigureuSyncConfig(this IHostBuilder hostBuilder, string filename = "usync.json")
        {
            return hostBuilder.ConfigureAppConfiguration((hostingContext, config) => {
                config.AdduSyncConfigs(hostingContext, filename);
            });
        }

    }
}
