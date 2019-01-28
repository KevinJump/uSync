using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Umbraco.Core;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core.Extensions;

namespace uSync8.BackOffice
{
    public class uSyncBackOfficeSettings
    {
        public string rootFolder { get; set; } = "~/uSync/v8/";

        public bool UseFlatStructure { get; set; } = true;
        public bool ImportAtStartup { get; set; } = false;

        public bool ExportAtStartup { get; set; } = false;
        public bool ExportOnSave { get; set; } = true;

        public List<uSyncHandlerConfig> Handlers { get; set; } = new List<uSyncHandlerConfig>();

        private string settingsFile = Umbraco.Core.IO.SystemDirectories.Config + "/uSync8.config";

        public void LoadSettings(SyncHandlerCollection syncHandlers)
        {
            this.Handlers = syncHandlers
                                .Select(x => new uSyncHandlerConfig(x, true))
                                .ToList();

            var node = GetSettingsFile();
            if (node == null)
            {
                SaveSettings();
                return; // everything will be default; 
            }

            this.rootFolder = node.Element("Folder").ValueOrDefault(rootFolder);
            this.UseFlatStructure = node.Element("FlatFolders").ValueOrDefault(true);
            this.ImportAtStartup = node.Element("ImportAtStartup").ValueOrDefault(true);
            this.ExportAtStartup = node.Element("ExportAtStartup").ValueOrDefault(false);
            this.ExportOnSave = node.Element("ExportOnSave").ValueOrDefault(true);

            var handlerConfig = node.Element("Handlers");
            var defaultHandlerEnabled = handlerConfig.Attribute("EnableMissing").ValueOrDefault(false);

            if (this.Handlers != null && handlerConfig != null)
            {
                foreach (var handler in this.Handlers)
                {
                    handler.Config.Enabled = defaultHandlerEnabled;

                    var handlerNode = handlerConfig.Elements("Handler").FirstOrDefault(x => x.Attribute("Alias").ValueOrDefault(string.Empty) == handler.Alias);
                    if (handlerNode != null)
                    {
                        LoadHandlerConfig(handlerNode, handler.Alias);
                    }
                }
            }

        }
        public void SaveSettings()
        {
            var node = GetSettingsFile(true);

            node.CreateOrSetElement("Folder", rootFolder);
            node.CreateOrSetElement("FlatFolders", UseFlatStructure);
            node.CreateOrSetElement("ImportAtStartup", ImportAtStartup);
            node.CreateOrSetElement("ExportAtStartup", ExportAtStartup);
            node.CreateOrSetElement("ExportOnSave", ExportOnSave);

            if (this.Handlers != null)
            {
                var handlerConfig = node.Element("Handlers");
                if (handlerConfig == null)
                {
                    handlerConfig = new XElement("Handlers",
                        new XAttribute("EnableMissing", true));
                    node.Add(handlerConfig);
                }


                foreach (var handler in Handlers)
                {
                    var handlerNode = handlerConfig.Elements("Handler").FirstOrDefault(x => x.Attribute("Alias").Value == handler.Alias);
                    if (handlerNode == null)
                    {
                        handlerNode = new XElement("Handler");
                        handlerConfig.Add(handlerNode);
                    }

                    SaveHandlerConfig(handlerNode, handler);
                }
            }

            SaveSettingsFile(node);
        }

        #region Default Handler Loading Stuff
        public uSyncHandlerSettings LoadHandlerConfig(XElement node, string alias)
        {
            if (node == null) return null;
            var nodeAlias = node.Attribute("Alias").ValueOrDefault("unknown");

            if (!alias.InvariantEquals(nodeAlias)) return null;

            var settings = new uSyncHandlerSettings();

            settings.Enabled = node.Attribute("Enabled").ValueOrDefault(true);

            var settingNode = node.Element("Settings");
            if (settingNode != null)
            {
                var handlerSettings = new Dictionary<string, string>();

                foreach (var setting in settingNode.Elements("Add"))
                {
                    var key = setting.Attribute("Key").ValueOrDefault(string.Empty);
                    var value = setting.Attribute("Value").ValueOrDefault(string.Empty);

                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                        continue;

                    handlerSettings.Add(key, value);
                }

                settings.Settings = handlerSettings;
            }

            return settings;
        }

        public void SaveHandlerConfig(XElement node, uSyncHandlerConfig config)
        {
            if (node == null) return;

            node.SetAttributeValue("Alias", config.Alias);
            node.SetAttributeValue("Enabled", config.Config.Enabled);

            if (config.Config.Settings != null && config.Config.Settings.Any())
            {
                var settingNode = new XElement("Settings");

                foreach (var setting in config.Config.Settings)
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
    }

    public class uSyncHandlerConfig
    {
        public string Alias { get; }
        public ISyncHandler Handler { get; private set; }
        public uSyncHandlerSettings Config { get; set; }

        public uSyncHandlerConfig(ISyncHandler handler, bool enabled)
        {
            Alias = handler.Alias;
            Handler = handler;
            Config = new uSyncHandlerSettings()
            {
                Enabled = enabled
            };
        }
    }

    public class uSyncHandlerSettings
    {
        public bool Enabled { get; set; }
        public string[] Actions { get; set; }

        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

    }
}
