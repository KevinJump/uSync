using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Core;

using uSync8.Core.Extensions;

namespace uSync8.BackOffice.Configuration
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncConfig
    {
        public uSyncSettings Settings { get; set; }

        private string settingsFile = Umbraco.Core.IO.SystemDirectories.Config + "/uSync8.config";

        public uSyncConfig()
        {
            this.Settings = LoadSettings();
        }

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

        private IList<HandlerSet> LoadHandlerSets(XElement node, uSyncSettings defaultSettings, out string defaultSet)
        {
            var sets = new List<HandlerSet>();
            defaultSet = ValueFromWebConfigOrDefault("DefaultHandlerSet", node.Attribute("Default").ValueOrDefault("default"));

            foreach (var setNode in node.Elements("Handlers"))
            {
                var handlerSet = LoadSingleHandlerSet(setNode, defaultSettings);
                if (handlerSet.Handlers.Count > 0)
                    sets.Add(handlerSet);
            }

            return sets;
        }

        private HandlerSet LoadSingleHandlerSet(XElement setNode, uSyncSettings defaultSettings)
        {
            var handlerSet = new HandlerSet();
            handlerSet.Name = setNode.Attribute("Name").ValueOrDefault("default");

            foreach (var handlerNode in setNode.Elements("Handler"))
            {
                var handlerSettings = LoadHandlerConfig(handlerNode, defaultSettings);
                if (handlerSettings != null)
                    handlerSet.Handlers.Add(handlerSettings);
            }

            return handlerSet;

        }


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

        private void SetHandlerValues(XElement node, HandlerSettings handler, uSyncSettings defaultSettings)
        {
            node.SetAttributeValue("Alias", handler.Alias);
            node.SetAttributeValue("Enabled", handler.Enabled);

            if (handler.GuidNames.IsOverridden)
                node.SetAttributeValue("GuidNames", handler.GuidNames.Value);

            if (handler.UseFlatStructure.IsOverridden)
                node.SetAttributeValue("UseFlatStructure", handler.UseFlatStructure.Value);

            if (handler.BatchSave.IsOverridden)
                node.SetAttributeValue("BatchSave", handler.BatchSave.Value);

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
            settings.BatchSave = GetLocalValue(node.Attribute("BatchSave"), defaultSettings.BatchSave);
            settings.Actions = node.Attribute("Actions").ValueOrDefault("All").ToDelimitedList().ToArray();

            // var settingNode = node.Element("Settings");
            // if (settingNode != null)
            // {
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
            // }

            return settings;
        }

        private OverriddenValue<TObject> GetLocalValue<TObject>(XAttribute attribute, TObject defaultValue)
        {
            if (attribute == null)
                return new OverriddenValue<TObject>(defaultValue, false);

            return new OverriddenValue<TObject>(attribute.ValueOrDefault(defaultValue), true);
        }

        public void SaveHandlerConfig(XElement node, HandlerSettings config, uSyncSettings globalSettings)
        {
            if (node == null) return;

            node.SetAttributeValue("Alias", config.Alias);
            node.SetAttributeValue("Enabled", config.Enabled);

            if (config.GuidNames.IsOverridden)
                node.SetAttributeValue("GuidNames", config.GuidNames);

            if (config.UseFlatStructure.IsOverridden)
                node.SetAttributeValue("UseFlatStructure", config.UseFlatStructure);

            if (config.BatchSave.IsOverridden)
                node.SetAttributeValue("BatchSave", config.BatchSave);

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

        private XElement GetSettingsFile(bool loadIfBlank = false)
        {
            var filePath = Umbraco.Core.IO.IOHelper.MapPath(settingsFile);
            if (File.Exists(filePath))
            {
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
                var file = new XElement("uSync",
                    new XElement("BackOffice"));
                return file.Element("BackOffice");
            }

            return null;
        }

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

        public string GetExtensionSetting(string app, string key, string defaultValue)
        {
            return GetExtensionSetting<string>(app, key, defaultValue);
        }

        public TObject GetExtensionSetting<TObject>(string app, string key, TObject defaultValue)
        {
            var node = GetSettingsFile();
            if (node == null) return defaultValue;

            var section = node.Element(app);
            if (section != null)
                return section.Element(key).ValueOrDefault(defaultValue);

            return defaultValue;

        }

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

        public void FlushSettings()
        {
            var node = GetSettingsFile();
            if (node == null) return;

            this.SaveSettingsFile(node);
        }

        #endregion

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
