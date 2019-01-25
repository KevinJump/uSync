using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("D0E0769D-CCAE-47B4-AD34-4182C587B08A", "Template Serializer", uSyncConstants.Serialization.Template)]
    public class TemplateSerializer : SyncSerializerBase<ITemplate>, ISyncSerializer<ITemplate>
    {

        private readonly IFileService fileService;

        public TemplateSerializer(IEntityService entityService, IFileService fileService) 
            : base(entityService)
        {
            this.fileService = fileService;
        }

        protected override SyncAttempt<ITemplate> DeserializeCore(XElement node)
        {
            if (!IsValid(node))
                throw new ArgumentException("Bad Xml Format");

            var alias = node.Element("Alias").ValueOrDefault(string.Empty);
            if (string.IsNullOrEmpty(alias))
                SyncAttempt<ITemplate>.Fail(node.Name.LocalName, ChangeType.Import, "No Alias");

            var name = node.Element("Name").ValueOrDefault(string.Empty);

            var item = default(ITemplate);
            var key = node.Element("Key").ValueOrDefault(Guid.Empty);
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
                    var content = System.IO.File.ReadAllText(templatePath);

                    item = new Template(name, alias);
                    item.Path = templatePath;
                    item.Content = content;
                }
                else
                {
                    // template is missing
                    // we can't create 
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

            var master = node.Element("Master").ValueOrDefault(string.Empty);
            if (master != string.Empty)
            {
                var masterItem = fileService.GetTemplate(master);
                if (masterItem != null)
                    item.SetMasterTemplate(masterItem);
            }

            fileService.SaveTemplate(item);

            return SyncAttempt<ITemplate>.Succeed(item.Name, item, ChangeType.Import);
        }

        private bool IsValid(XElement node)
        {
            if (node == null || node.Element("Alias") == null || node.Element("Name") == null)
                return false;

            return true;
        }

        protected override SyncAttempt<XElement> SerializeCore(ITemplate item)
        {
            var node = this.InitializeBaseNode(item);

            node.Add(new XElement("Name", item.Name));
            node.Add(new XElement("Key", item.Key));
            node.Add(new XElement("Alias", item.Alias));
            node.Add(new XElement("Master", item.MasterTemplateAlias));

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(ITemplate), ChangeType.Export);
        }
    }
}
