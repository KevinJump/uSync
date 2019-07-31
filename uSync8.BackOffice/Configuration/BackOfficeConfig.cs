using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Core;
using uSync8.Core.Extensions;

namespace uSync8.BackOffice.Configuration
{
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

            settings.RootFolder = node.Element("Folder").ValueOrDefault(settings.RootFolder);
            settings.UseFlatStructure = node.Element("FlatFolders").ValueOrDefault(true);
            settings.ImportAtStartup = node.Element("ImportAtStartup").ValueOrDefault(true);
            settings.ExportAtStartup = node.Element("ExportAtStartup").ValueOrDefault(false);
            settings.ExportOnSave = node.Element("ExportOnSave").ValueOrDefault(true);
            settings.UseGuidNames = node.Element("UseGuidFilenames").ValueOrDefault(false);
            settings.BatchSave = node.Element("BatchSave").ValueOrDefault(false);
            settings.ReportDebug = node.Element("ReportDebug").ValueOrDefault(false);
            settings.AddOnPing = node.Element("AddOnPing").ValueOrDefault(true);

            var handlerConfig = node.Element("Handlers");

            if (handlerConfig != null && handlerConfig.HasElements)
            {
                settings.EnableMissingHandlers = handlerConfig.Attribute("EnableMissing").ValueOrDefault(true);

                foreach (var handlerNode in handlerConfig.Elements("Handler"))
                {
                    var handlerSetting = LoadHandlerConfig(handlerNode, settings);

                    if (handlerSetting != null)
                        settings.Handlers.Add(handlerSetting);
                }
            }

            return settings;
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

            if (settings.Handlers != null && settings.Handlers.Any())
            {
                var handlerConfig = node.Element("Handlers");
                if (handlerConfig == null)
                {
                    handlerConfig = new XElement("Handlers",
                        new XAttribute("EnableMissing", true));
                    node.Add(handlerConfig);
                }


                foreach (var handler in settings.Handlers)
                {
                    if (!handler.GuidNames.IsOverridden)
                        handler.GuidNames.SetDefaultValue(settings.UseGuidNames);

                    if (!handler.UseFlatStructure.IsOverridden)
                        handler.UseFlatStructure.SetDefaultValue(settings.UseFlatStructure);

                    if (!handler.BatchSave.IsOverridden)
                        handler.BatchSave.SetDefaultValue(settings.BatchSave);

                    var handlerNode = handlerConfig.Elements("Handler").FirstOrDefault(x => x.Attribute("Alias").Value == handler.Alias);
                    if (handlerNode == null)
                    {
                        handlerNode = new XElement("Handler");
                        handlerConfig.Add(handlerNode);
                    }

                    SaveHandlerConfig(handlerNode, handler, settings);
                }
            }
            else
            {
                // if the handlers is null, we should write out the handlers we have loaded,
                // so that there is something in the config


            }
            SaveSettingsFile(node);

            if (fireReload)
            {
                this.Settings = LoadSettings();
                Reloaded?.Invoke(settings);
            }

            return settings;
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

            var settingNode = node.Element("Settings");
            if (settingNode != null)
            {
                var perHandlerSettings = new Dictionary<string, string>();

                foreach (var settingItem in settingNode.Elements("Add"))
                {
                    var key = settingItem.Attribute("Key").ValueOrDefault(string.Empty);
                    var value = settingItem.Attribute("Value").ValueOrDefault(string.Empty);

                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                        continue;

                    perHandlerSettings.Add(key, value);
                }

                settings.Settings = perHandlerSettings;
            }

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
    }

}
