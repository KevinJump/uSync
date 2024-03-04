using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace uSync;

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
            Path.Combine(Path.GetDirectoryName(filename) ?? string.Empty, Path.GetFileNameWithoutExtension(filename));

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
}
