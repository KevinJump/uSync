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
        private readonly IUserService userService;

        public InitCommand(TextReader reader, TextWriter writer,
            DatabaseBuilder databaseBuilder,
            IGlobalSettings globalSettings,
            IUserService userService) : base(reader, writer)
        {
            this.databaseBuilder = databaseBuilder;
            this.globalSettings = globalSettings;
            this.userService = userService;

            AdvancedHelp = HelpTextResource.Init_Help;
        }


        public async Task<SyncCommandResult> Run(string[] args)
        {
            await writer.WriteLineAsync($" Umbraco is at level {Current.RuntimeState.Level}");

            if (Current.RuntimeState.Level == RuntimeLevel.Run)
            {
                await writer.WriteLineAsync(" Umbraco is alread installed");
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
                if (result != SyncCommandResult.Success)
                    return result;
            }

            CreateDatabase();

            var parameters = args.Where(x => !x.StartsWith("-")).ToArray();
            if (parameters.Length == 2)
            {
                result = SetupAdmin(parameters[0], parameters[1]);
            }

            await writer.WriteLineAsync(" Setup complete !!! #h5yr\n");
            return result;
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
            }

            return SyncCommandResult.Success;
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

        private SyncCommandResult SetupAdmin(string username, string password)
        {
            var admin = userService.GetUserById(Constants.Security.SuperUserId);
            if (admin == null)
            {
                throw new InvalidOperationException(" Could not find the super user!");
            }

            writer.WriteLine(" Found super user - setting password ");

            var membershipUser = GetCurrentProvider().GetUser(Constants.Security.SuperUserId, true);
            if (membershipUser == null)
            {
                throw new InvalidOperationException($" No user found in membership provider with id of {Constants.Security.SuperUserId}.");
            }

            try
            {
                writer.WriteLine(" Setting password", password);
                var success = membershipUser.ChangePassword("default", password);
                if (success == false)
                {
                    throw new FormatException(" Password must be at least " + GetCurrentProvider().MinRequiredPasswordLength + " characters long and contain at least " + GetCurrentProvider().MinRequiredNonAlphanumericCharacters + " symbols");
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(ex.ToString());
                // throw new FormatException(" Password must be at least " + CurrentProvider.MinRequiredPasswordLength + " characters long and contain at least " + CurrentProvider.MinRequiredNonAlphanumericCharacters + " symbols");
            }

            writer.WriteLine(" setting super user username - email -name");

            admin.Email = username;
            admin.Name = username;
            admin.Username = username;

            writer.WriteLine(" Saving user");
            userService.Save(admin);

            return SyncCommandResult.Success;
        }

        private MembershipProvider GetCurrentProvider()
        {
            var provider = Umbraco.Core.Security.MembershipProviderExtensions.GetUsersMembershipProvider();
            return provider;
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


    }
}
