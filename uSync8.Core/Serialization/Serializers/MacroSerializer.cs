using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("CBB3FDA1-F7B3-470E-B78F-EB316576C8C6", "Macro Serializer", uSyncConstants.Serialization.Macro)]
    public class MacroSerializer : SyncSerializerBase<IMacro>, ISyncSerializer<IMacro>
    {
        private readonly IMacroService macroService;

        public MacroSerializer(
            IEntityService entityService,
            IMacroService macroService) 
            : base(entityService)
        {
            this.macroService = macroService;
        }

        protected override SyncAttempt<IMacro> DeserializeCore(XElement node)
        {
            if (node.Element("Name") == null)
                throw new ArgumentNullException("XML missing Name parameter");

            var item = default(IMacro);

            var key = node.GetKey();
            var alias = node.GetAlias();
            var name = node.Element("Name").ValueOrDefault(string.Empty);

            var macroSource = node.Element("MacroSource").ValueOrDefault(string.Empty);
            var macroType = node.Element("MacroType").ValueOrDefault(MacroTypes.PartialView);

            item = macroService.GetById(key);


            if (item == null)
            {
                item = macroService.GetByAlias(alias);
            }

            if (item == null)
            {
                item = new Macro(alias, name, macroSource, macroType);
            }

            item.Name = name;
            item.Alias = alias;
            item.MacroSource = macroSource;
            item.MacroType = macroType;

            item.UseInEditor = node.Element("UseInEditor").ValueOrDefault(false);
            item.DontRender = node.Element("DontRender").ValueOrDefault(false);
            item.CacheByMember = node.Element("CachedByMember").ValueOrDefault(false);
            item.CacheByPage = node.Element("CachedByPage").ValueOrDefault(false);
            item.CacheDuration = node.Element("CachedDuration").ValueOrDefault(0);

            var properties = node.Element("Properties");
            if (properties != null && properties.HasElements)
            {
                foreach(var propNode in properties.Elements("Property"))
                {
                    var propertyAlias = propNode.Element("Alias").ValueOrDefault(string.Empty);
                    var editorAlias = propNode.Element("EditorAlias").ValueOrDefault(string.Empty);
                    var propertyName = propNode.Element("Name").ValueOrDefault(string.Empty);
                    var sortOrder = propNode.Element("SortOrder").ValueOrDefault(0);

                    if (item.Properties.ContainsKey(propertyAlias))
                    {
                        item.Properties.UpdateProperty(propertyAlias, propertyName, sortOrder, editorAlias);
                    }
                    else
                    {
                        item.Properties.Add(new MacroProperty(propertyAlias, propertyName, sortOrder, editorAlias));
                    }
                }
            }

            macroService.Save(item);

            return SyncAttempt<IMacro>.Succeed(item.Name, item, ChangeType.Import);
        }


        protected override SyncAttempt<XElement> SerializeCore(IMacro item)
        {
            var node = this.InitializeBaseNode(item, item.Alias);

            node.Add(new XElement("Name", item.Name));
            node.Add(new XElement("MacroSource", item.MacroSource));
            node.Add(new XElement("MacroType", item.MacroType));
            node.Add(new XElement("UseInEditor", item.UseInEditor));
            node.Add(new XElement("DontRender", item.DontRender));
            node.Add(new XElement("CachedByMember", item.CacheByMember));
            node.Add(new XElement("CachedByPage", item.CacheByPage));
            node.Add(new XElement("CachedDuration", item.CacheDuration));

            var properties = new XElement("Properties");
            foreach(var property in item.Properties)
            {
                properties.Add(new XElement("Property",
                    new XElement("Key", property.Key),
                    new XElement("Name", property.Name)),
                    new XElement("Alias", property.Alias),
                    new XElement("SortOrder", property.SortOrder),
                    new XElement("EditorAlias", property.EditorAlias));
            }

            node.Add(properties);

            return SyncAttempt<XElement>.SucceedIf(
                node != null,
                item.Name,
                node,
                typeof(IMacro),
                ChangeType.Export);
        }

        protected override IMacro GetItem(Guid key)
            => macroService.GetById(key);

        protected override IMacro GetItem(string alias)
            => macroService.GetByAlias(alias);

    }
}
