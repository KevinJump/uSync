using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Models;
using uSync.Core.Versions;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("D0E0769D-CCAE-47B4-AD34-4182C587B08A", "Template Serializer", uSyncConstants.Serialization.Template)]
    public class TemplateSerializer : SyncSerializerBase<ITemplate>, ISyncSerializer<ITemplate>
    {
        private readonly IFileService _fileService;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly IFileSystem _viewFileSystem;

        private readonly uSyncCapabilityChecker _capabilityChecker;
        private readonly IConfiguration _configuration;

        [Obsolete("call with compatibility checker - will remove in v11")]
        public TemplateSerializer(
            IEntityService entityService,
            ILogger<TemplateSerializer> logger,
            IShortStringHelper shortStringHelper,
            IFileService fileService,
            FileSystems fileSystems
        ) : this(entityService, logger, shortStringHelper, fileService, fileSystems, null, null)
        { }

        [ActivatorUtilitiesConstructor]
        public TemplateSerializer(
            IEntityService entityService,
            ILogger<TemplateSerializer> logger,
            IShortStringHelper shortStringHelper,
            IFileService fileService,
            FileSystems fileSystems,
            IConfiguration configuration,
            uSyncCapabilityChecker capabilityChecker)
            : base(entityService, logger)
        {
            this._fileService = fileService;
            this._shortStringHelper = shortStringHelper;

            _viewFileSystem = fileSystems.MvcViewsFileSystem;
            _configuration = configuration;
            _capabilityChecker = capabilityChecker;
        }

        protected override SyncAttempt<ITemplate> ProcessDelete(Guid key, string alias, SerializerFlags flags)
        {
            if (flags.HasFlag(SerializerFlags.LastPass))
            {
                logger.LogDebug("Processing deletes as part of the last pass");
                return base.ProcessDelete(key, alias, flags);
            }

            logger.LogDebug("Delete not processing as this is not the final pass");
            return SyncAttempt<ITemplate>.Succeed(alias, ChangeType.Hidden);
        }

        protected override SyncAttempt<ITemplate> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var key = node.GetKey();
            var alias = node.GetAlias();

            var name = node.Element("Name").ValueOrDefault(string.Empty);
            var item = default(ITemplate);

            var details = new List<uSyncChange>();

            if (key != Guid.Empty)
                item = _fileService.GetTemplate(key);

            item ??= _fileService.GetTemplate(alias);

            if (item == null)
            {
                item = new Template(_shortStringHelper, name, alias);
                details.AddNew(alias, alias, "Template");

                if (ShouldGetContentFromNode(node, options))
                {
                    logger.LogDebug("Getting content for Template from XML");
                    item.Content = GetContentFromConfig(node);
                }
                else
                {
                    logger.LogDebug("Loading template content from disk");

                    var templatePath = ViewPath(alias);
                    if (_viewFileSystem.FileExists(templatePath))
                    {
                        logger.LogDebug("Reading {path} contents", templatePath);
                        item.Content = GetContentFromFile(templatePath);
                        item.Path = templatePath;
                    }
                    else
                    {
                        if (!ViewsAreCompiled(options))
                        {
                            // template is missing
                            // we can't create 
                            logger.LogWarning("Failed to create template {path} the local file is missing", templatePath);
                            return SyncAttempt<ITemplate>.Fail(name, ChangeType.Import, $"The template {templatePath} file is missing.");
                        }
                        else
                        {
                            // template is not on disk, we could use the viewEngine to find the view 
                            // if this finds the view it tells us that the view is somewhere else ? 

                            logger.LogDebug("Failed to find content, but UsingRazorViews so will create anyway, then delete the file");
                            item.Content = $"<!-- [uSyncMarker:{this.Id}]  template content - will be removed -->";
                        }
                    }
                }
            }

            if (item == null)
            {
                // creating went wrong
                logger.LogWarning("Failed to create template");
                return SyncAttempt<ITemplate>.Fail(name, ChangeType.Import, "Failed to create template");
            }

            if (item.Key != key)
            {
                details.AddUpdate(uSyncConstants.Xml.Key, item.Key, key);
                item.Key = key;
            }

            if (item.Name != name)
            {
                details.AddUpdate(uSyncConstants.Xml.Name, item.Name, name);
                item.Name = name;
            }

            if (item.Alias != alias)
            {
                details.AddUpdate(uSyncConstants.Xml.Alias, item.Alias, alias);
                item.Alias = alias;
            }

            if (ShouldGetContentFromNode(node, options))
            {
                var content = GetContentFromConfig(node);
                if (content != item.Content)
                {
                    details.AddUpdate("Content", item.Content, content);
                    item.Content = content;
                }
            }

            //var master = node.Element("Parent").ValueOrDefault(string.Empty);
            //if (master != string.Empty)
            //{
            //    var masterItem = fileService.GetTemplate(master);
            //    if (masterItem != null)
            //        item.SetMasterTemplate(masterItem);
            //}

            // Deserialize now takes care of the save.
            // fileService.SaveTemplate(item);

            return SyncAttempt<ITemplate>.Succeed(item.Name, item, ChangeType.Import, details);
        }

        /// <summary>
        ///  As a default if the file contains the content node, then we are going to use it 
        ///  for the content. if it doesn't then we are not.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private bool ShouldGetContentFromNode(XElement node, SyncSerializerOptions options)
        {
            if (_capabilityChecker != null 
                && _configuration != null 
                && _capabilityChecker.HasRuntimeMode)
            {
                if (node.Element("Contents") != null)
                {
                    if (ViewsAreCompiled(options))
                    {
                        logger.LogDebug("Template contents will not be imported because site is running in Production mode");
                        return false;
                    }

                    // else (we have content - not running in Production) 
                    return true;
                }

                // else (we don't have content it doesn't matter)
                return false;
            }

            // default 
            return node.Element("Contents") != null;
            // && options.GetSetting(uSyncConstants.Conventions.IncludeContent, false);
        }

        public string GetContentFromConfig(XElement node)
            => node.Element("Contents").ValueOrDefault(string.Empty);

        public string GetContentFromFile(string templatePath)
        {
            var content = "";
            using (var stream = _viewFileSystem.OpenFile(templatePath))
            {
                using (var sr = new StreamReader(stream))
                {
                    content = sr.ReadToEnd();
                    sr.Close();
                    sr.Dispose();
                }

                stream.Close();
                stream.Dispose();
            }

            return content;
        }

        public override SyncAttempt<ITemplate> DeserializeSecondPass(ITemplate item, XElement node, SyncSerializerOptions options)
        {
            var details = new List<uSyncChange>();
            var saved = false;

            var master = node.Element("Parent").ValueOrDefault(string.Empty);
            if (master != string.Empty && item.MasterTemplateAlias != master)
            {
                logger.LogDebug("Looking for master {master}", master);
                var masterItem = _fileService.GetTemplate(master);
                if (masterItem != null && item.MasterTemplateAlias != master)
                {
                    details.AddUpdate("Parent", item.MasterTemplateAlias, master);

                    logger.LogDebug("Setting Master {alias}", masterItem.Alias);
                    item.SetMasterTemplate(masterItem);

                    SaveItem(item);
                    saved = true;
                }
            }

            if (ViewsAreCompiled(options))
            {
                // using razor views - we delete the template file at the end (because its in a razor view). 
                var templatePath = ViewPath(item.Alias);
                if (_viewFileSystem.FileExists(templatePath))
                {
                    var fullPath = _viewFileSystem.GetFullPath(templatePath);

                    if (System.IO.File.Exists(fullPath))
                    {
                        var content = System.IO.File.ReadAllText(fullPath);
                        if (content.Contains($"[uSyncMarker:{this.Id}]"))
                        {
                            logger.LogDebug("Removing the file from disk, because it exists in a razor view {templatePath}", templatePath);
                            _viewFileSystem.DeleteFile(templatePath);

                            // we have to tell the handlers we saved it - or they will and write the file back 
                            return SyncAttempt<ITemplate>.Succeed(item.Name, item, ChangeType.Import, "Razor view removed", true, details);
                        }
                    }
                }
            }

            return SyncAttempt<ITemplate>.Succeed(item.Name, item, ChangeType.Import, "", saved, details);
        }


        protected override SyncAttempt<XElement> SerializeCore(ITemplate item, SyncSerializerOptions options)
        {
            var node = this.InitializeBaseNode(item, item.Alias, this.CalculateLevel(item));

            node.Add(new XElement("Name", item.Name));
            node.Add(new XElement("Parent", item.MasterTemplateAlias));

            if (options.GetSetting(uSyncConstants.Conventions.IncludeContent, false))
            {
                node.Add(SerializeContent(item));
            }

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(ITemplate), ChangeType.Export);
        }

        private XElement SerializeContent(ITemplate item)
        {
            return new XElement("Contents", new XCData(item.Content ?? string.Empty));
        }

        private int CalculateLevel(ITemplate item)
        {
            if (item.MasterTemplateAlias.IsNullOrWhiteSpace()) return 1;

            int level = 1;
            var current = item;
            while (!string.IsNullOrWhiteSpace(current.MasterTemplateAlias) && level < 20)
            {
                level++;
                var parent = _fileService.GetTemplate(current.MasterTemplateAlias);
                if (parent == null) return level;

                current = parent;
            }

            return level;
        }

        public override ITemplate FindItem(int id)
            => _fileService.GetTemplate(id);

        public override ITemplate FindItem(string alias)
            => _fileService.GetTemplate(alias);

        public override ITemplate FindItem(Guid key)
            => _fileService.GetTemplate(key);

        public override void SaveItem(ITemplate item)
            => _fileService.SaveTemplate(item);

        public override void Save(IEnumerable<ITemplate> items)
            => _fileService.SaveTemplate(items);

        public override void DeleteItem(ITemplate item)
            => _fileService.DeleteTemplate(item.Alias);

        public override string ItemAlias(ITemplate item)
            => item.Alias;

        /// <summary>
        ///  we clean the content out of the template,
        ///  We don't care if the content has changed during a normal serialization
        /// </summary>
        protected override XElement CleanseNode(XElement node)
        {
            var contentNode = node.Element("Content");
            if (contentNode != null) contentNode.Remove();

            return base.CleanseNode(node);
        }


        private string ViewPath(string alias)
            => _viewFileSystem.GetRelativePath(alias.Replace(" ", "") + ".cshtml");

        private bool ViewsAreCompiled(SyncSerializerOptions options)
            => _configuration.IsUmbracoRunningInProductionMode()
                || options.GetSetting("UsingRazorViews", false);
    }
}
