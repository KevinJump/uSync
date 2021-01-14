using System;
using System.Configuration;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Security;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Exceptions;
using Umbraco.Core.IO;
using Umbraco.Core.Migrations.Install;
using Umbraco.Core.Services;

using uSync8.BackOffice.Commands;

namespace uSync.BaseCommands
{
    /// <summary>
    ///  Installs umbraco, assuming you have the connection string setup.
    /// </summary>
    /// <remarks>
    ///  this command will install umbraco, if the connection string is set,
    ///  if both the connection string and the version are set, but the database
    ///  isn't initlized then you get BootFailed.
    ///  
    ///  when that happens the install command will blank the version in the web.config
    ///  and then restart and try again.
    ///  
    ///  as the end it can also set the username/password combo (if passed on the command line)
    /// </remarks>
    [SyncCommand("Init", "init", "Initilizes the umbraco installation, (as long the connection string is set)")]
    public class InitCommand : SyncCommandBase, ISyncCommand
    {
        private readonly DatabaseBuilder databaseBuilder;
        private readonly IGlobalSettings globalSettings;

        private readonly SyncUserHelper userHelper;

        public InitCommand(TextReader reader, TextWriter writer,
            DatabaseBuilder databaseBuilder,
            IGlobalSettings globalSettings,
            SyncUserHelper userHelper,
            IUserService userService) : base(reader, writer)
        {
            this.databaseBuilder = databaseBuilder;
            this.globalSettings = globalSettings;

            this.userHelper = userHelper;

            AdvancedHelp = HelpTextResource.Init_Help;
        }


        public async Task<SyncCommandResult> Run(string[] args)
        {
            // await writer.WriteLineAsync($" Umbraco is at level {Current.RuntimeState.Level}");

            if (Current.RuntimeState.Level == RuntimeLevel.Run)
            {
                await writer.WriteLineAsync(" Umbraco is alread installed");

                if (SupportsUmbracoUnattended() && AdminUserNeedsUpdate(args))
                {
                    await writer.WriteLineAsync(" Admin user still needs updating, flicking back to install mode.");
                    SaveSetting(Constants.AppSettings.ConfigurationStatus, "");
                    return SyncCommandResult.Restart;
                }

                return SyncCommandResult.NoResult;
            }

            var connectionString = ConfigurationManager.ConnectionStrings[Constants.System.UmbracoConnectionName];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                await writer.WriteLineAsync(" No connection string in config file");
                if (args.InvariantContains("-sqlce"))
                {
                    await writer.WriteLineAsync(" Putting the basic SQL CE Connection string in. ");
                    SaveConnectionString(DatabaseBuilder.EmbeddedDatabaseConnectionString, Constants.DbProviderNames.SqlCe);
                    connectionString = new ConnectionStringSettings(Constants.System.UmbracoConnectionName, DatabaseBuilder.EmbeddedDatabaseConnectionString, Constants.DbProviderNames.SqlCe);
                    return SyncCommandResult.Restart;
                }
                else
                {
                    await writer.WriteLineAsync(" you can use the -sqlce switch to have this value automatically populated");
                    return SyncCommandResult.Error;
                }

            }

            var result = SyncCommandResult.Success;

            if (connectionString.ProviderName.Equals("System.Data.SqlServerCe.4.0"))
            {
                // create the sql db file.
                result = CreateSqlCEDb(connectionString.ConnectionString);
                if (result < SyncCommandResult.Success) return result;

                if (SupportsUmbracoUnattended())
                {
                    switch(result)
                    {
                        case SyncCommandResult.Success:
                            SaveSetting(Constants.AppSettings.ConfigurationStatus, UmbracoVersion.SemanticVersion.ToSemanticString());
                            SaveSetting("Umbraco.Core.RuntimeState.InstallUnattended", "true");
                           return SyncCommandResult.Restart;
                        case SyncCommandResult.NoResult:
                            await UpdateAdminUser(args);
                            SaveSetting(Constants.AppSettings.ConfigurationStatus, UmbracoVersion.SemanticVersion.ToSemanticString());
                            return SyncCommandResult.Restart;
                    }
                }
            }

            if (!SupportsUmbracoUnattended())
            {
                await writer.WriteLineAsync(" creating db (pre 8.11)");
                CreateDatabase();
                return await UpdateAdminUser(args);

            }

            await writer.WriteLineAsync(" Setup complete !!! #h5yr\n");
            return result;
        }

        private async Task<SyncCommandResult> UpdateAdminUser(string[] args)
        {
            var parameters = args.Where(x => !x.StartsWith("-")).ToArray();
            if (parameters.Length == 2)
            {
                return await userHelper.SetupAdminUser(parameters[0], parameters[1]);
            }

            return SyncCommandResult.NoResult;

        }

        public bool AdminUserNeedsUpdate(string [] args)
        {
            var parameters = args.Where(x => !x.StartsWith("-")).ToArray();
            if (parameters.Length == 2)
            {
                return userHelper.AdminUserNeedsaUpdate(parameters[0]);
            }

            return false;
        }


        private SyncCommandResult CreateSqlCEDb(string connectionString)
        {
            var dataDirectory = (string)AppDomain.CurrentDomain.GetData("DataDirectory");

            var parts = connectionString.Split(';');
            var dataSource = parts.FirstOrDefault(x => x.ToLower().Contains("data source"));

            if (string.IsNullOrWhiteSpace(dataSource))
            {
                writer.WriteLine(" Data source value missing");
                return SyncCommandResult.Error;
            }

            var fileName = dataSource.Split('=')
                .Last()
                .Split('\\')
                .Last()
                .Trim();

            var path = Path.Combine(dataDirectory, fileName);

            if (!File.Exists(path))
            {
                var engine = new SqlCeEngine(connectionString);
                engine.CreateDatabase();
                writer.WriteLine(" Created SQL CE Database");
                return SyncCommandResult.Success;
            }

            return SyncCommandResult.NoResult;
        }

        private SyncCommandResult CreateDatabase()
        {
            writer.WriteLine(" Creating Database Schema");

            var result = databaseBuilder.CreateSchemaAndData();
            if (!result.Success)
            {
                writer.WriteLine(" Failed to create db {0}", result.Message);
                return SyncCommandResult.Error;
            }

            if (globalSettings != null)
            {
                writer.WriteLine(" Setting Umbraco Version in config");
                globalSettings.ConfigurationStatus = UmbracoVersion.SemanticVersion.ToSemanticString();
            }
            else
            {
                writer.WriteLine(" Global Settings, not set");
            }

            return result.Success ? SyncCommandResult.Restart : SyncCommandResult.Error;
        }

        /// <summary>
        ///  same a setting back to the web.config 
        /// </summary>
        /// <remarks>
        ///  From Umbraco.Core 
        /// </remarks>
        private void SaveSetting(string key, string value)
        {
            var fileName = IOHelper.MapPath(string.Format("{0}/web.config", SystemDirectories.Root));
            var xml = XDocument.Load(fileName, LoadOptions.PreserveWhitespace);

            var appSettings = xml.Root.DescendantsAndSelf("appSettings").Single();

            // Update appSetting if it exists, or else create a new appSetting for the given key and value
            var setting = appSettings.Descendants("add").FirstOrDefault(s => s.Attribute("key").Value == key);
            if (setting == null)
                appSettings.Add(new XElement("add", new XAttribute("key", key), new XAttribute("value", value)));
            else
                setting.Attribute("value").Value = value;

            xml.Save(fileName, SaveOptions.DisableFormatting);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private static void SaveConnectionString(string connectionString, string providerName)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullOrEmptyException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(providerName)) throw new ArgumentNullOrEmptyException(nameof(providerName));

            var fileSource = "web.config";
            var fileName = IOHelper.MapPath(SystemDirectories.Root + "/" + fileSource);

            var xml = XDocument.Load(fileName, LoadOptions.PreserveWhitespace);
            if (xml.Root == null) throw new Exception($"Invalid {fileSource} file (no root).");

            var connectionStrings = xml.Root.DescendantsAndSelf("connectionStrings").FirstOrDefault();
            if (connectionStrings == null) throw new Exception($"Invalid {fileSource} file (no connection strings).");

            // handle configSource
            var configSourceAttribute = connectionStrings.Attribute("configSource");
            if (configSourceAttribute != null)
            {
                fileSource = configSourceAttribute.Value;
                fileName = IOHelper.MapPath(SystemDirectories.Root + "/" + fileSource);

                if (!File.Exists(fileName))
                    throw new Exception($"Invalid configSource \"{fileSource}\" (no such file).");

                xml = XDocument.Load(fileName, LoadOptions.PreserveWhitespace);
                if (xml.Root == null) throw new Exception($"Invalid {fileSource} file (no root).");

                connectionStrings = xml.Root.DescendantsAndSelf("connectionStrings").FirstOrDefault();
                if (connectionStrings == null) throw new Exception($"Invalid {fileSource} file (no connection strings).");
            }

            // create or update connection string
            var setting = connectionStrings.Descendants("add").FirstOrDefault(s => s.Attribute("name")?.Value == Constants.System.UmbracoConnectionName);
            if (setting == null)
            {
                connectionStrings.Add(new XElement("add",
                    new XAttribute("name", Constants.System.UmbracoConnectionName),
                    new XAttribute("connectionString", connectionString),
                    new XAttribute("providerName", providerName)));
            }
            else
            {
                AddOrUpdateAttribute(setting, "connectionString", connectionString);
                AddOrUpdateAttribute(setting, "providerName", providerName);
            }

            // save
            xml.Save(fileName, SaveOptions.DisableFormatting);
        }

        private static void AddOrUpdateAttribute(XElement element, string name, string value)
        {
            var attribute = element.Attribute(name);
            if (attribute == null)
                element.Add(new XAttribute(name, value));
            else
                attribute.Value = value;
        }

        private bool SupportsUmbracoUnattended()
            => UmbracoVersion.SemanticVersion >= new Semver.SemVersion(8, 11, 0, prerelease: "rc");
    }
}
