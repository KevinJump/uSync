using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.IO;
using System.Linq;

using uSync.BackOffice.Configuration;

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
            return hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AdduSyncConfigs(hostingContext, filename);
            });
        }

        public static JToken GetNonDefaultValues<T>(this T source, T defaults)
        {
            try
            {
                var sourceJson = JObject.FromObject(source);
                if (defaults == null) return sourceJson;

                var defaultJson = JObject.FromObject(defaults);

                var changes = new JObject();

                foreach (var key in sourceJson.Properties().Select(x => x.Name))
                {
                    var sourceObject = sourceJson[key];

                    if (sourceObject is JObject)
                    {
                        changes.Add(key, sourceObject.GetNonDefaultValues(defaultJson[key]));
                    }
                    else
                    {

                        var sourceValue = JsonConvert.SerializeObject(sourceJson[key]);
                        var defaultValue = JsonConvert.SerializeObject(defaultJson[key]);

                        if (!sourceValue.Equals(defaultValue))
                        {
                            changes.Add(key, sourceJson[key]);
                        }
                    }
                }

                return changes;

            }
            catch(Exception ex)
            {
                var x = ex;
            }

            return null;
        }
    }
}
