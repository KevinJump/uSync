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
    /// <summary>
    ///  Extensions to help reading/writing uSync settings
    /// </summary>
    public static class uSyncConfigExtensions
    {
        /// <summary>
        ///  Add the suite of usync.json files to the config tree.
        /// </summary>
        /// <remarks>
        ///  lets you have usync.json, usync.environment.json and usync.machinename.json files
        ///  that are loaded along side the other config sources, so you can move uSync config
        ///  out of the appsettings.json files if you want.
        /// </remarks>
        public static void AdduSyncConfigs(this IConfigurationBuilder configurationBuilder, HostBuilderContext builderContext, string filename)
        {
            var env = builderContext.HostingEnvironment;
            var fileRootName =
                Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));

            configurationBuilder.AddJsonFile($"{fileRootName}.json", optional: true, reloadOnChange: true);
            configurationBuilder.AddJsonFile($"{fileRootName}.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
            configurationBuilder.AddJsonFile($"{fileRootName}.{Environment.MachineName}.json", optional: true, reloadOnChange: true);
        }

        /// <summary>
        ///  add usync.json files sources to the applications configuration
        /// </summary>
        /// <remarks>
        ///  allows you to move the uSync config out of the main appsettings.config file, by default will add 
        ///  usync.json, usync.[environment].json and usync.[machinename].json to the list of configuration
        ///  locations
        /// </remarks>
        /// <param name="hostBuilder">Host builder object from Configure method</param>
        /// <param name="filename">name of json file to add (default usync.json)</param>
        public static IHostBuilder ConfigureuSyncConfig(this IHostBuilder hostBuilder, string filename = "usync.json")
        {
            return hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AdduSyncConfigs(hostingContext, filename);
            });
        }

        /// <summary>
        ///  returns values from a object that are not set to the default values from another object.
        /// </summary>
        /// <remarks>
        ///  this extension method gives us a way to see which values have been changed from the 
        ///  default values by comparing to objects. and returning a JToken value that only
        ///  contains the changed values - we use this to show users what has changed in their 
        ///  configuration.
        /// </remarks>
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
