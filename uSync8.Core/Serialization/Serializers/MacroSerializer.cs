using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Logging;
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
            IEntityService entityService, ILogger logger,
            IMacroService macroService)
            : base(entityService, logger)
        {
            this.macroService = macroService;
        }

        protected override SyncAttempt<IMacro> DeserializeCore(XElement node)
        {
            var changes = new List<uSyncChange>();

            if (node.Element("Name") == null)
                throw new ArgumentNullException("XML missing Name parameter");

            var item = default(IMacro);

            var key = node.GetKey();
            var alias = node.GetAlias();
            var name = node.Element("Name").ValueOrDefault(string.Empty);

            var macroSource = node.Element("MacroSource").ValueOrDefault(string.Empty);
            var macroType = node.Element("MacroType").ValueOrDefault(MacroTypes.PartialView);

            logger.Debug<IMacro>("Macro by Key [{0}]", key);
            item = macroService.GetById(key);

            if (item == null)
            {
                logger.Debug<IMacro>("Macro by Alias [{0}]", key);
                item = macroService.GetByAlias(alias);
            }

            if (item == null)
            {
                logger.Debug<IMacro>("Creating New [{0}]", key);
                item = new Macro(alias, name, macroSource, macroType);
                changes.Add(uSyncChange.Create(alias, name, "New Macro"));
            }

            item.Name = name;
            item.Alias = alias;
            item.MacroSource = macroSource;
            item.MacroType = macroType;


            item.UseInEditor = node.Element("UseInEditor").ValueOrDefault(false);
            item.DontRender =  node.Element("DontRender").ValueOrDefault(false);
            item.CacheByMember = node.Element("CachedByMember").ValueOrDefault(false);
            item.CacheByPage = node.Element("CachedByPage").ValueOrDefault(false);
            item.CacheDuration = node.Element("CachedDuration").ValueOrDefault(0);

            var properties = node.Element("Properties");
            if (properties != null && properties.HasElements)
            {
                foreach (var propNode in properties.Elements("Property"))
                {
                    var propertyAlias = propNode.Element("Alias").ValueOrDefault(string.Empty);
                    var editorAlias = propNode.Element("EditorAlias").ValueOrDefault(string.Empty);
                    var propertyName = propNode.Element("Name").ValueOrDefault(string.Empty);
                    var sortOrder = propNode.Element("SortOrder").ValueOrDefault(0);

                    var propPath = $"{alias}: {propertyName}";

                    if (item.Properties.ContainsKey(propertyAlias))
                    {
                        item.Properties.UpdateProperty(propertyAlias, propertyName, sortOrder, editorAlias);
                    }
                    else
                    {
                        changes.Add(uSyncChange.Create(propPath, "Property", propertyAlias));
                        item.Properties.Add(new MacroProperty(propertyAlias, propertyName, sortOrder, editorAlias));
                    }
                }

            }

            RemoveOrphanProperties(item, properties);

            macroService.Save(item);

            var attempt = SyncAttempt<IMacro>.Succeed(item.Name, item, ChangeType.Import);
            if (changes.Any())
                attempt.Details = changes;

            return attempt;
        }

        private void RemoveOrphanProperties(IMacro item, XElement properties)
        {
            var removalKeys = new List<string>();
            if (properties == null)
            {
                removalKeys = item.Properties.Keys.ToList();
            }
            else
            {
                var aliases = properties.Elements("Property")
                    .Where(x => x.Element("Alias").ValueOrDefault(string.Empty) != string.Empty)
                    .Select(x => x.Element("Alias").ValueOrDefault(string.Empty))
                    .ToList();

                removalKeys = item.Properties.Values.Where(x => !aliases.Contains(x.Alias))
                    .Select(x => x.Alias)
                    .ToList();
            }

            foreach (var propKey in removalKeys)
            {
                item.Properties.Remove(propKey);
            }

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
            foreach (var property in item.Properties)
            {
                properties.Add(new XElement("Property",
                    new XElement("Key", property.Key),
                    new XElement("Name", property.Name),
                    new XElement("Alias", property.Alias),
                    new XElement("SortOrder", property.SortOrder),
                    new XElement("EditorAlias", property.EditorAlias)));
            }

            node.Add(properties);

            return SyncAttempt<XElement>.SucceedIf(
                node != null,
                item.Name,
                node,
                typeof(IMacro),
                ChangeType.Export);
        }

        protected override IMacro FindItem(Guid key)
            => macroService.GetById(key);

        protected override IMacro FindItem(string alias)
            => macroService.GetByAlias(alias);

    }
}
