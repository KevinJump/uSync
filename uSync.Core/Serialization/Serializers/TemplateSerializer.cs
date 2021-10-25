using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Models;

using static Umbraco.Cms.Core.Constants;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("D0E0769D-CCAE-47B4-AD34-4182C587B08A", "Template Serializer", uSyncConstants.Serialization.Template)]
    public class TemplateSerializer : SyncSerializerBase<ITemplate>, ISyncSerializer<ITemplate>
    {
        private readonly IFileService fileService;
        private readonly IShortStringHelper shortStringHelper;
        private readonly IHostingEnvironment hostEnvrionment;

        public TemplateSerializer(IEntityService entityService, ILogger<TemplateSerializer> logger,
            IShortStringHelper shortStringHelper,
            IFileService fileService,
            IHostingEnvironment hostEnvironment)
            : base(entityService, logger)
        {
            this.fileService = fileService;
            this.shortStringHelper = shortStringHelper;
            this.hostEnvrionment = hostEnvironment;
        }

        protected override SyncAttempt<ITemplate> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var key = node.GetKey();
            var alias = node.GetAlias();

            var name = node.Element("Name").ValueOrDefault(string.Empty);
            var item = default(ITemplate);

            var details = new List<uSyncChange>();

            if (key != Guid.Empty)
                item = fileService.GetTemplate(key);

            if (item == null)
                item = fileService.GetTemplate(alias);

            if (item == null)
            {
                var templatePath = hostEnvrionment.MapPathContentRoot(SystemDirectories.MvcViews + "/" + alias + ".cshtml");
                if (System.IO.File.Exists(templatePath))
                {
                    logger.LogDebug("Reading {0} contents", templatePath);
                    var content = System.IO.File.ReadAllText(templatePath);
                    item = new Template(shortStringHelper, name, alias);
                    item.Path = templatePath;
                    item.Content = content;

                    details.AddNew(alias, alias, "Template");
                }
                else
                {
                    // template is missing
                    // we can't create 
                    return SyncAttempt<ITemplate>.Fail(name, ChangeType.Import, $"The template {templatePath} file is missing.");
                }
            }

            if (item == null)
            {
                // creating went wrong
                return SyncAttempt<ITemplate>.Fail(name, ChangeType.Import, "Failed to create template");
            }

            if (item.Key != key)
            {
                details.AddUpdate("Key", item.Key, key);
                item.Key = key;
            }

            if (item.Name != name)
            {
                details.AddUpdate("Name", item.Name, name);
                item.Name = name;
            }

            if (item.Alias != alias)
            {
                details.AddUpdate("Alias", item.Alias, alias);
                item.Alias = alias;
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

        public override SyncAttempt<ITemplate> DeserializeSecondPass(ITemplate item, XElement node, SyncSerializerOptions options)
        {
            var details = new List<uSyncChange>();

            var master = node.Element("Parent").ValueOrDefault(string.Empty);
            if (master != string.Empty && item.MasterTemplateAlias != master)
            {
                logger.LogDebug("Looking for master {0}", master);
                var masterItem = fileService.GetTemplate(master);
                if (masterItem != null && item.MasterTemplateAlias != master)
                {
                    details.AddUpdate("Parent", item.MasterTemplateAlias, master);

                    logger.LogDebug("Setting Master {0}", masterItem.Alias);
                    item.SetMasterTemplate(masterItem);

                    if (!options.Flags.HasFlag(SerializerFlags.DoNotSave))
                        SaveItem(item);
                }
            }

            return SyncAttempt<ITemplate>.Succeed(item.Name, item, ChangeType.Import, details);
        }


        protected override SyncAttempt<XElement> SerializeCore(ITemplate item, SyncSerializerOptions options)
        {
            var node = this.InitializeBaseNode(item, item.Alias, this.CalculateLevel(item));

            node.Add(new XElement("Name", item.Name));
            node.Add(new XElement("Parent", item.MasterTemplateAlias));

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(ITemplate), ChangeType.Export);
        }

        private int CalculateLevel(ITemplate item)
        {
            if (item.MasterTemplateAlias.IsNullOrWhiteSpace()) return 1;

            int level = 1;
            var current = item;
            while (!string.IsNullOrWhiteSpace(current.MasterTemplateAlias) && level < 20)
            {
                level++;
                var parent = fileService.GetTemplate(current.MasterTemplateAlias);
                if (parent == null) return level;

                current = parent;
            }

            return level;
        }

        public override ITemplate FindItem(int id)
            => fileService.GetTemplate(id);

        public override ITemplate FindItem(string alias)
            => fileService.GetTemplate(alias);

        public override ITemplate FindItem(Guid key)
            => fileService.GetTemplate(key);

        public override void SaveItem(ITemplate item)
            => fileService.SaveTemplate(item);

        public override void Save(IEnumerable<ITemplate> items)
            => fileService.SaveTemplate(items);

        public override void DeleteItem(ITemplate item)
            => fileService.DeleteTemplate(item.Alias);

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
    }
}
