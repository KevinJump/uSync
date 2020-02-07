using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using uSync8.BackOffice.Commands;
using System.Threading;

namespace uSync.ConsoleApp
{
    /// <summary>
    ///  entry point
    /// </summary>
    /// <remarks>
    ///  most of this is how Chauffeur does it. 
    ///        https://github.com/aaronpowell/Chauffeur
    ///        
    ///  If you want a command line less tied into uSync, with a lot more 
    ///  robust extension points take a look that.
    /// </remarks>
    class Program
    {
        const string uSyncAppDomain = "uSync-AppDomain";

        static void Main(string[] args)
        {
            if (AppDomain.CurrentDomain.FriendlyName == uSyncAppDomain)
            {
                Console.WriteLine($"uSync Umbraco Command Line [" +
                    $"{Assembly.GetExecutingAssembly().GetName().Version}] ");

                Console.Write("Initialising...");

                var consoleHost = new ConsoleHost(Console.In, Console.Out);
                var task = consoleHost.Run(args);
                task.Wait();

                if (task.Result == SyncCommandResult.Restart)
                {
                    Console.Write("Restarting...");
                    InitApplication(args);
                }
            }
            else
            {
                InitApplication(args);
            }
        }

        /// <summary>
        ///  finds, the umbraco folder, and intializes the app
        /// </summary>
        /// <remarks>
        ///  All of this code borrows heviely from Chauffeur
        ///        https://github.com/aaronpowell/Chauffeur
        /// </remarks>
        /// <param name="args"></param>
        static void InitApplication(string[] args)
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            var exePath = Path.GetDirectoryName(currentAssembly.Location);
            var siteRoot = FindSiteRoot(exePath);

            var configPath = Path.Combine(siteRoot, "web.config");
            var setup = new AppDomainSetup
            {
                ConfigurationFile = configPath,
                ApplicationBase = siteRoot,
                PrivateBinPath = exePath
            };

            var domain = AppDomain.CreateDomain(uSyncAppDomain,
                AppDomain.CurrentDomain.Evidence, setup);

            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (File.Exists(assembly.FullName))
                    domain.Load(assembly.FullName);
            }

            domain.SetData("DataDirectory", Path.Combine(siteRoot, "App_Data"));
            domain.SetData(".appDomain", "From Domain");
            domain.SetData(".appId", "From Domain");
            domain.SetData(".appVPath", exePath);
            domain.SetData(".appPath", exePath);
            domain.SetData(".appDomain", "From Domain");

            var thread = Thread.GetDomain();

            thread.SetData(".appDomain", "From Thread");
            thread.SetData(".appId", "From Thread");
            thread.SetData(".appVPath", exePath);
            thread.SetData(".appPath", exePath);

            var thisAssembly = new FileInfo(currentAssembly.Location);
            domain.ExecuteAssembly(thisAssembly.FullName, args);
        }


        /// <summary>
        ///  go up the tree, until we find the folder with the web.config in it
        /// </summary>
        static string FindSiteRoot(string path)
        {
            var folder = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(folder))
                throw new EntryPointNotFoundException("Can't find the umbraco folder");

            if (File.Exists(Path.Combine(folder, "web.config"))) return folder;


            return FindSiteRoot(folder);

        }
    }
}
