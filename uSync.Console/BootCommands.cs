using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.IO;

using uSync.BaseCommands;

using uSync8.BackOffice.Commands;

namespace uSync.ConsoleApp
{
    /// <summary>
    ///  Dealing with the special case when you have a BootFail, 
    ///  but still want to try and run some commands.
    /// </summary>
    public class BootCommands
    {
        private readonly TextReader reader;
        private readonly TextWriter writer;
        private readonly IFactory factory;

        public BootCommands(TextReader reader, TextWriter writer,
            IFactory factory)
        {
            this.reader = reader;
            this.writer = writer;
            this.factory = factory;
        }

        /// <summary>
        /// Special process we do when boot fails. 
        /// </summary>
        /// <remarks>
        ///  this lets us do things like run the Init command
        ///  event though we haven't been able to boot up umbraco.
        ///  
        ///  a boot fail might occur when one or both of the UmbracoVersion
        ///  and Umbraco Connection string are set, but the DB isn't there
        ///  or setup.
        ///  
        ///  by processing a boot fail we can 'fix' those settings, restart
        ///  and boot into the proper setup.
        /// </remarks>
        public async Task<SyncCommandResult> RunBootUpSteps(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "init":
                    return await BootInit(args);
                case "quit":
                    return await BootQuit(args);
                default:
                    await writer.WriteLineAsync(" Umbraco is not setup, only 'init' and 'quit' commands will work at the moment");
                    return SyncCommandResult.Complete;
            }
        }

        private async Task<SyncCommandResult> BootInit(string[] args)
        {
            if (Current.RuntimeState.Level < RuntimeLevel.Install)
            {
                await writer.WriteLineAsync(" Cannot initialize, because umbraco won't boot...");
                if (Current.RuntimeState.BootFailedException
                    .Message.InvariantContains("A connection string is configured but"))
                {
                    // connection string fail.
                    await writer.WriteLineAsync(" Boot failed for connection string, attempting to fix");
                    var configVersion = ConfigurationManager.AppSettings[Constants.AppSettings.ConfigurationStatus];
                    if (!string.IsNullOrWhiteSpace(configVersion))
                    {
                        await writer.WriteLineAsync(" The config version isn't blank, this migh be stopping us from booting");
                        await writer.WriteLineAsync(" Cleaning the config version, then run usync init again...");
                        SaveSetting(Constants.AppSettings.ConfigurationStatus, "");
                        return SyncCommandResult.Restart;

                    }
                    else
                    {
                        await writer.WriteLineAsync($"  Boot failed for a reason not to do with being not setup:\n {Current.RuntimeState.BootFailedException}");
                        await writer.WriteLineAsync("\n  We can't setup this site until it isn't booting for the right reasons (missing db)");
                    }

                    return SyncCommandResult.Error;
                }
            }

            // if we get here, then we will be and Install level.
            try
            {
                var installCommand = factory.GetInstance<InitCommand>();
                return await installCommand.Run(args.Skip(1).ToArray());
            }
            catch (InvalidOperationException ex)
            {
                await writer.WriteLineAsync($" Failed to run initilization - {ex.Message}");
                return SyncCommandResult.Error;
            }
        }

        private async Task<SyncCommandResult> BootQuit(string[] args)
        {
            await writer.WriteLineAsync("Quitting...");
            return SyncCommandResult.Complete;
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

    }
}
