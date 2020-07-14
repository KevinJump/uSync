using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("D0E0769D-CCAE-47B4-AD34-4182C587B08A", "Template Serializer", uSyncConstants.Serialization.Template)]
    public class TemplateSerializer : SyncSerializerBase<ITemplate>, ISyncOptionsSerializer<ITemplate>
    {
        private readonly IFileService fileService;

        public TemplateSerializer(IEntityService entityService, ILogger logger,
            IFileService fileService) 
            : base(entityService, logger)
        {
            this.fileService = fileService;
        }

        protected override SyncAttempt<ITemplate> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var key = node.GetKey();
            var alias = node.GetAlias();

            var name = node.Element("Name").ValueOrDefault(string.Empty);
            var item = default(ITemplate);

            if (key != Guid.Empty)
                item = fileService.GetTemplate(key);

            if (item == null)
                item = fileService.GetTemplate(alias);

            if (item == null)
            {
                // create 
                var templatePath = IOHelper.MapPath(SystemDirectories.MvcViews + "/" + alias.ToSafeFileName() + ".cshtml");
                if (System.IO.File.Exists(templatePath))
                {
                    logger.Debug<TemplateSerializer>("Reading {0} contents", templatePath);
                    var content = System.IO.File.ReadAllText(templatePath);
                    item = new Template(name, alias);
                    item.Path = templatePath;
                    item.Content = content;
                }
                else
                {
                    // template is missing
                    // we can't create 
                    return SyncAttempt<ITemplate>.Fail(name, ChangeType.Import, "The template '.cshtml' file is missing.");
                }
            }

            if (item == null)
            {
                // creating went wrong
                return SyncAttempt<ITemplate>.Fail(name, ChangeType.Import, "Failed to create template");
            }

            if (item.Key != key)
                item.Key = key;

            if (item.Name != name)
                item.Name = name;

            if (item.Alias != alias)
                item.Alias = alias;

            //var master = node.Element("Parent").ValueOrDefault(string.Empty);
            //if (master != string.Empty)
            //{
            //    var masterItem = fileService.GetTemplate(master);
            //    if (masterItem != null)
            //        item.SetMasterTemplate(masterItem);
            //}

            // Deserialize now takes care of the save.
            // fileService.SaveTemplate(item);

            return SyncAttempt<ITemplate>.Succeed(item.Name, item, ChangeType.Import);
        }

        public override SyncAttempt<ITemplate> DeserializeSecondPass(ITemplate item, XElement node, SyncSerializerOptions options)
        {
            var master = node.Element("Parent").ValueOrDefault(string.Empty);
            if (master != string.Empty && item.MasterTemplateAlias != master)
            {
                logger.Debug<TemplateSerializer>("Looking for master {0}", master);
                var masterItem = fileService.GetTemplate(master);
                if (masterItem != null)
                {
                    logger.Debug<TemplateSerializer>("Setting Master {0}", masterItem.Alias);
                    item.SetMasterTemplate(masterItem);

                    if (!options.Flags.HasFlag(SerializerFlags.DoNotSave))
                        SaveItem(item);
                }
            }

            return SyncAttempt<ITemplate>.Succeed(item.Name, item, ChangeType.Import);
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


        protected override ITemplate FindItem(string alias)
            => fileService.GetTemplate(alias);

        protected override ITemplate FindItem(Guid key)
            => fileService.GetTemplate(key);

        protected override void SaveItem(ITemplate item)
            => fileService.SaveTemplate(item);

        public override void Save(IEnumerable<ITemplate> items)
            => fileService.SaveTemplate(items);

        protected override void DeleteItem(ITemplate item)
            => fileService.DeleteTemplate(item.Alias);

        protected override string ItemAlias(ITemplate item)
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
