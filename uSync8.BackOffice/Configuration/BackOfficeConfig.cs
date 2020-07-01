﻿using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Core;
using Umbraco.Core.Logging;

using uSync8.Core.Extensions;

namespace uSync8.BackOffice.Configuration
{
    /// <summary>
    ///  uSync config loading / saving
    /// </summary>
    /// <remarks>
    ///  We could just serialize this. but we don't because we don't want to wipe 
    ///  settings from the file that we don't know about (yet). 
    ///  
    ///  In theory extensions can save settings in this file if they want to, so we 
    ///  don't know the final schema. 
    ///  
    ///  doing this also alows us to load values from the web.config appSettings to
    ///  override our own settings which helps people when deploying. 
    ///  
    ///  So instead we write each value/node out to the file individually. this gives 
    ///  us flexiblity in what can go in the file, but if does make bits of the code
    ///  looky messy with the repeated ValueFrom... lines.
    /// </remarks>

    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncConfig
    {
        private readonly IProfilingLogger logger;

        public uSyncSettings Settings { get; set; }

        private string settingsFile = Umbraco.Core.IO.SystemDirectories.Config + "/uSync8.config";

        public uSyncConfig(IProfilingLogger logger)
        {
            this.logger = logger;
            this.Settings = LoadSettings();
        }

        /// <summary>
        /// Load the uSync settings from disk.
        /// </summary>
        public uSyncSettings LoadSettings()
        {
            uSyncSettings settings = new uSyncSettings();

            var node = GetSettingsFile();
            if (node == null)
            {
                return SaveSettings(settings);
            }

            settings.RootFolder = ValueFromWebConfigOrDefault("Folder", node.Element("Folder").ValueOrDefault(settings.RootFolder));
            settings.UseFlatStructure = ValueFromWebConfigOrDefault("FlatFolders", node.Element("FlatFolders").ValueOrDefault(true));
            settings.ImportAtStartup = ValueFromWebConfigOrDefault("ImportAtStartup", node.Element("ImportAtStartup").ValueOrDefault(true));
            settings.ExportAtStartup = ValueFromWebConfigOrDefault("ExportAtStartup", node.Element("ExportAtStartup").ValueOrDefault(false));
            settings.ExportOnSave = ValueFromWebConfigOrDefault("ExportOnSave", node.Element("ExportOnSave").ValueOrDefault(true));
            settings.UseGuidNames = ValueFromWebConfigOrDefault("UseGuidFilenames", node.Element("UseGuidFilenames").ValueOrDefault(false));
            settings.BatchSave = node.Element("BatchSave").ValueOrDefault(false);
            settings.ReportDebug = node.Element("ReportDebug").ValueOrDefault(false);
            settings.AddOnPing = node.Element("AddOnPing").ValueOrDefault(true);
            settings.RebuildCacheOnCompletion = node.Element("RebuildCacheOnCompletion").ValueOrDefault(false);
            settings.FailOnMissingParent = node.Element("FailOnMissingParent").ValueOrDefault(true);

            // load the handlers 
            var handlerSets = node.Element("HandlerSets");
            if (handlerSets != null)
            {
                settings.HandlerSets = LoadHandlerSets(handlerSets, settings, out string defaultSet);
                settings.DefaultSet = defaultSet;
            }
            else
            {
                var handlers = node.Element("Handlers");
                if (handlers != null)
                {
                    // old config, load as default :( 
                    settings.HandlerSets = new List<HandlerSet>();
                    var defaultSet = LoadSingleHandlerSet(handlers, settings);
                    settings.HandlerSets.Add(defaultSet);
                }
            }

            // fire the loaded event, so things can tell when they are loaded. 
            Reloaded?.Invoke(settings);

            return settings;
        }

        /// <summary>
        ///  Load the Handler sets from the `Handlers` section of the file
        /// </summary>
        private IList<HandlerSet> LoadHandlerSets(XElement node, uSyncSettings defaultSettings, out string defaultSet)
        {
            var sets = new List<HandlerSet>();
            defaultSet = ValueFromWebConfigOrDefault("DefaultHandlerSet", node.Attribute("Default").ValueOrDefault("default"));

            logger.Debug<uSyncConfig>("Handlers : Default Set {defaultSet}", defaultSet);

            foreach (var setNode in node.Elements("Handlers"))
            {
                var handlerSet = LoadSingleHandlerSet(setNode, defaultSettings);
                if (handlerSet.Handlers.Count > 0)
                    sets.Add(handlerSet);
            }

            return sets;
        }

        /// <summary>
        ///  Load a handler set (a collection of handlers)
        /// </summary>
        /// <remarks>
        ///  each handler set has a name, it lets us have diffrent sets for diffrent tasks
        /// </remarks>
        private HandlerSet LoadSingleHandlerSet(XElement setNode, uSyncSettings defaultSettings)
        {
            var handlerSet = new HandlerSet();
            handlerSet.Name = setNode.Attribute("Name").ValueOrDefault("default");

            foreach (var handlerNode in setNode.Elements("Handler"))
            {
                var handlerSettings = LoadHandlerConfig(handlerNode, defaultSettings);
                if (handlerSettings != null)
                {
                    logger.Debug<uSyncConfig>("Loading Handler {alias} {enabled} [{actions}]",
                        handlerSettings.Alias, handlerSettings.Enabled, string.Join(",", handlerSettings.Actions));

                    handlerSet.Handlers.Add(handlerSettings);
                }
            }

            return handlerSet;

        }

        /// <summary>
        ///  Save the settings to disk.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="fireReload"></param>
        /// <returns></returns>
        public uSyncSettings SaveSettings(uSyncSettings settings, bool fireReload = false)
        {
            var node = GetSettingsFile(true);

            node.CreateOrSetElement("Folder", settings.RootFolder);
            node.CreateOrSetElement("FlatFolders", settings.UseFlatStructure);
            node.CreateOrSetElement("ImportAtStartup", settings.ImportAtStartup);
            node.CreateOrSetElement("ExportAtStartup", settings.ExportAtStartup);
            node.CreateOrSetElement("ExportOnSave", settings.ExportOnSave);
            node.CreateOrSetElement("UseGuidFilenames", settings.UseGuidNames);
            node.CreateOrSetElement("BatchSave", settings.BatchSave);
            node.CreateOrSetElement("ReportDebug", settings.ReportDebug);
            node.CreateOrSetElement("RebuildCacheOnCompletion", settings.RebuildCacheOnCompletion);
            node.CreateOrSetElement("FailOnMissingParent", settings.FailOnMissingParent);

            if (settings.HandlerSets.Count > 0)
            {
                // remove the existing handlerSets node?
                var handlerSets = node.FindOrCreate("HandlerSets");
                handlerSets.SetAttributeValue("Default", settings.DefaultSet);

                foreach (var set in settings.HandlerSets)
                {
                    // find the handler node for this set. 
                    var setNode = handlerSets.FindOrCreate("Handlers", "Name", set.Name);

                    foreach (var handler in set.Handlers)
                    {
                        var handlerNode = setNode.FindOrCreate("Handler", "Alias", handler.Alias);
                        SetHandlerValues(handlerNode, handler, settings);
                    }

                    // remove and missing handlers (so on disk but not in settings)
                    setNode.RemoveMissingElements("Handler", "Alias", set.Handlers.Select(x => x.Alias));
                }

                // remove missing HandlerSets (on disk not in settings)
                handlerSets.RemoveMissingElements("Handlers", "Name", settings.HandlerSets.Select(x => x.Name));

                var legacyNode = node.Element("Handlers");
                if (legacyNode != null)
                    legacyNode.Remove();
            }

            SaveSettingsFile(node);

            if (fireReload)
            {
                this.Settings = LoadSettings();
            }

            return settings;
        }

        /// <summary>
        ///  Set the values for a handler 
        /// </summary>
        private void SetHandlerValues(XElement node, HandlerSettings handler, uSyncSettings defaultSettings)
        {
            node.SetAttributeValue("Alias", handler.Alias);
            node.SetAttributeValue("Enabled", handler.Enabled);

            if (handler.GuidNames.IsOverridden)
                node.SetAttributeValue("GuidNames", handler.GuidNames.Value);

            if (handler.UseFlatStructure.IsOverridden)
                node.SetAttributeValue("UseFlatStructure", handler.UseFlatStructure.Value);

            if (handler.FailOnMissingParent.IsOverridden)
                node.SetAttributeValue("FailOnMissingParent", handler.FailOnMissingParent.Value);

            node.SetAttributeValue("Actions", string.Join(",", handler.Actions));

            if (handler.Settings != null && handler.Settings.Count > 0)
            {
                var settingsNode = node.FindOrCreate("Settings");

                foreach (var setting in handler.Settings)
                {
                    var s = settingsNode.FindOrCreate("Add", "Key", setting.Key);
                    s.SetAttributeValue("Value", setting.Value);
                }

                settingsNode.RemoveMissingElements("Add", "Key", handler.Settings.Keys.ToList());
            }
        }

        #region Default Handler Loading Stuff

        /// <summary>
        ///  load the config for a single handler. 
        /// </summary>
        public HandlerSettings LoadHandlerConfig(XElement node, uSyncSettings defaultSettings)
        {
            if (node == null) return null;
            var alias = node.Attribute("Alias").ValueOrDefault(string.Empty);

            if (string.IsNullOrEmpty(alias)) return null;

            var enabled = node.Attribute("Enabled").ValueOrDefault(true);

            var settings = new HandlerSettings(alias, enabled);

            // these values can be set locally, but if they aren't 
            // we get them from the global setting.
            settings.GuidNames = GetLocalValue(node.Attribute("GuidNames"), defaultSettings.UseGuidNames);
            settings.UseFlatStructure = GetLocalValue(node.Attribute("UseFlatStructure"), defaultSettings.UseFlatStructure);
            settings.Actions = node.Attribute("Actions").ValueOrDefault("All").ToDelimitedList().ToArray();
            settings.FailOnMissingParent = GetLocalValue(node.Attribute("FailOnMissingParent"), defaultSettings.FailOnMissingParent);

            // handlers can have their own indivual settings beneath a node
            //
            // <Handler Alias="sample" ... >
            //   <Add Key="settingName" Value="settingValue" />
            // </Handler>
            //
            // These get added to a Settings dictionary for the handler, so it 
            // can access them as needed (they are often also passed to serializers)
            // 
            var perHandlerSettings = new Dictionary<string, string>();

            foreach (var settingItem in node.Elements("Add"))
            {
                var key = settingItem.Attribute("Key").ValueOrDefault(string.Empty);
                var value = settingItem.Attribute("Value").ValueOrDefault(string.Empty);

                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    continue;

                perHandlerSettings.Add(key, value);
            }

            settings.Settings = perHandlerSettings;

            return settings;
        }

        /// <summary>
        ///  Get the value of an attribute, and check if its overriding the default
        /// </summary>
        /// <remarks>
        ///  Handlers can override default values in the settings, but if the handler value
        ///  is the same as the default, we want to know - so when we save the settings we 
        ///  don't create reduntant settings under a handler that match the default 
        /// </remarks>
        private OverriddenValue<TObject> GetLocalValue<TObject>(XAttribute attribute, TObject defaultValue)
        {
            if (attribute == null)
                return new OverriddenValue<TObject>(defaultValue, false);

            return new OverriddenValue<TObject>(attribute.ValueOrDefault(defaultValue), true);
        }

        /// <summary>
        ///  Save a handler config back to the XElement node
        /// </summary>
        public void SaveHandlerConfig(XElement node, HandlerSettings config, uSyncSettings globalSettings)
        {
            if (node == null) return;

            node.SetAttributeValue("Alias", config.Alias);
            node.SetAttributeValue("Enabled", config.Enabled);

            if (config.GuidNames.IsOverridden)
                node.SetAttributeValue("GuidNames", config.GuidNames);

            if (config.UseFlatStructure.IsOverridden)
                node.SetAttributeValue("UseFlatStructure", config.UseFlatStructure);

            if (config.FailOnMissingParent.IsOverridden)
                node.SetAttributeValue("FailOnMissingParent", config.FailOnMissingParent);

            if (config.Actions.Length > 0 && !(config.Actions.Length == 1 && config.Actions[0].InvariantEquals("all")))
                node.SetAttributeValue("Actions", string.Join(",", config.Actions));

            if (config.Settings != null && config.Settings.Any())
            {
                var settingNode = new XElement("Settings");

                foreach (var setting in config.Settings)
                {
                    settingNode.Add(new XElement("Add",
                        new XAttribute("Key", setting.Key),
                        new XAttribute("Value", setting.Value)));
                }

                var existing = node.Element("Settings");
                if (existing != null) existing.Remove();

                node.Add(settingNode);
            }
        }

        #endregion

        /// <summary>
        ///  Locate the settings file and load it into an XElement
        /// </summary>
        private XElement GetSettingsFile(bool loadIfBlank = false)
        {
            var filePath = Umbraco.Core.IO.IOHelper.MapPath(settingsFile);
            if (File.Exists(filePath))
            {
                logger.Debug<uSyncConfig>("Loading Settings from {filePath}", filePath);

                var node = XElement.Load(filePath);
                var settingsNode = node.Element("BackOffice");
                if (settingsNode != null)
                    return settingsNode;

                if (loadIfBlank)
                {
                    node.Add(new XElement("BackOffice"));
                    return node.Element("BackOffice");
                }
                else
                {
                    return null;
                }
            }

            if (loadIfBlank)
            {
                logger.Debug<uSyncConfig>("Loading Blank (default) settings");

                var file = new XElement("uSync",
                    new XElement("BackOffice"));
                return file.Element("BackOffice");
            }

            return null;
        }

        /// <summary>
        ///  Save the settings node back to the disk.
        /// </summary>
        /// <param name="node"></param>
        private void SaveSettingsFile(XElement node)
        {
            var root = node.Parent;
            if (root != null)
            {
                var filePath = Umbraco.Core.IO.IOHelper.MapPath(settingsFile);
                root.Save(filePath);
            }
        }

        public static event uSyncSettingsEvent Reloaded;

        public delegate void uSyncSettingsEvent(uSyncSettings settings);

        #region extension settings 

        /// <summary>
        ///  Get a setting for an extension
        /// </summary>
        /// <param name="app">name of extension</param>
        /// <param name="key">key value</param>
        /// <param name="defaultValue">default value to use if setting is missing</param>
        public string GetExtensionSetting(string app, string key, string defaultValue)
        {
            return GetExtensionSetting<string>(app, key, defaultValue);
        }

        /// <summary>
        ///  Get a setting for an extension
        /// </summary>
        /// <remarks>
        ///  Code will look for a section node based on the appName, then get the key from 
        ///  within it. 
        ///  
        /// <code><![CDATA[
        ///   <app>
        ///     <key>value</key>
        ///   </app>
        /// ]]>
        /// </code>
        /// </remarks>
        /// <param name="app">name of extension</param>
        /// <param name="key">key value</param>
        /// <param name="defaultValue">default value to use if setting is missing</param>
        public TObject GetExtensionSetting<TObject>(string app, string key, TObject defaultValue)
        {
            var node = GetSettingsFile();
            if (node == null) return defaultValue;

            var section = node.Element(app);
            if (section != null)
                return section.Element(key).ValueOrDefault(defaultValue);

            return defaultValue;

        }

        /// <summary>
        ///  Save an extension setting to disk.
        /// </summary>
        public void SaveExtensionSetting<TObject>(string app, string key, TObject value)
        {
            var node = GetSettingsFile();
            if (node == null) return;

            var section = node.Element(app);
            if (section == null)
            {
                section = new XElement(app);
                node.Add(section);
            }

            var keyNode = section.Element(key);
            if (keyNode == null)
            {
                keyNode = new XElement(app);
                section.Add(keyNode);
            }

            keyNode.Value = value.ToString();
            SaveSettingsFile(node);
        }

        /// <summary>
        ///  Flush the settings back to disk.
        /// </summary>
        public void FlushSettings()
        {
            var node = GetSettingsFile();
            if (node == null) return;

            this.SaveSettingsFile(node);
        }

        #endregion


        /// <summary>
        ///  Gets a value from the web.config AppSettings if present, from uSync8.config if not.
        /// </summary>
        /// <param name="alias">setting alias (uSync. will be appended when looking in web.config)</param>
        private TObject ValueFromWebConfigOrDefault<TObject>(string alias, TObject defaultValue)
        {
            var result = ConfigurationManager.AppSettings[$"uSync.{alias.ToFirstUpper()}"];
            if (result != null)
            {
                var attempt = result.TryConvertTo<TObject>();
                if (attempt)
                    return attempt.Result;

            }
            return defaultValue;
        }

    }

}
