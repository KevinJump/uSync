using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using uSync;

namespace Umbraco.Cms.Web.UI.NetCore
{
    public class Program
    {
        public static void Main(string[] args)
            => CreateHostBuilder(args)
                .Build()
                .Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)

                // a uSync own config (usync.json in root by default) 
                // if this is omitted then settings can still be set in appsettings.json
                // its only needed if you want to move the config somewhere else.
                .ConfigureuSyncConfig()

                .ConfigureLogging(x => x.ClearProviders())
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
