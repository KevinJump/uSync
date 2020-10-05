using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("CBB3FDA1-F7B3-470E-B78F-EB316576C8C6", "Macro Serializer", uSyncConstants.Serialization.Macro)]
    public class MacroSerializer : SyncSerializerBase<IMacro>, ISyncOptionsSerializer<IMacro>
    {
        private readonly IMacroService macroService;

        public MacroSerializer(
            IEntityService entityService, ILogger logger,
            IMacroService macroService)
            : base(entityService, logger)
        {
            this.macroService = macroService;
        }

        protected override SyncAttempt<IMacro> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var details = new List<uSyncChange>();

            if (node.Element("Name") == null)
                throw new ArgumentNullException("XML missing Name parameter");

            var item = default(IMacro);

            var key = node.GetKey();
            var alias = node.GetAlias();
            var name = node.Element("Name").ValueOrDefault(string.Empty);

            var macroSource = node.Element("MacroSource").ValueOrDefault(string.Empty);
            var macroType = node.Element("MacroType").ValueOrDefault(MacroTypes.PartialView);

            logger.Debug<MacroSerializer>("Macro by Key [{0}]", key);
            item = macroService.GetById(key);

            if (item == null)
            {
                logger.Debug<MacroSerializer>("Macro by Alias [{0}]", key);
                item = macroService.GetByAlias(alias);
            }

            if (item == null)
            {
                logger.Debug<MacroSerializer>("Creating New [{0}]", key);
                item = new Macro(alias, name, macroSource, macroType);
                details.Add(uSyncChange.Create(alias, name, "New Macro"));
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

            if (item.MacroSource != macroSource)
            {
                details.AddUpdate("MacroSource", item.MacroSource, macroSource);
                item.MacroSource = macroSource;
            }

            if (item.MacroType != macroType)
            {
                details.AddUpdate("MacroType", item.MacroType, macroType);
                item.MacroType = macroType;
            }

            var useInEditor = node.Element("UseInEditor").ValueOrDefault(false);
            var dontRender =  node.Element("DontRender").ValueOrDefault(false);
            var cacheByMember = node.Element("CachedByMember").ValueOrDefault(false);
            var cacheByPage = node.Element("CachedByPage").ValueOrDefault(false);
            var cacheDuration = node.Element("CachedDuration").ValueOrDefault(0);

            if (item.UseInEditor != useInEditor)
            {
                details.AddUpdate("UseInEditor", item.UseInEditor, useInEditor);
                item.UseInEditor = useInEditor;
            }

            if (item.DontRender != dontRender)
            {
                details.AddUpdate("DontRender", item.DontRender, dontRender);
                item.DontRender = dontRender;
            }

            if (item.CacheByMember != cacheByMember)
            {
                details.AddUpdate("CacheByMember", item.CacheByMember, cacheByMember);
                item.CacheByMember = cacheByMember;
            }


            if (item.CacheByPage != cacheByPage)
            {
                details.AddUpdate("CacheByPage", item.CacheByPage, cacheByPage);
                item.CacheByPage = cacheByPage;
            }


            if (item.CacheDuration != cacheDuration)
            {
                details.AddUpdate("CacheByMember", item.CacheDuration, cacheDuration);
                item.CacheDuration = cacheDuration;
            }


            var properties = node.Element("Properties");
            if (properties != null && properties.HasElements)
            {
                foreach (var propNode in properties.Elements("Property"))
                {

                    var propertyAlias = propNode.Element("Alias").ValueOrDefault(string.Empty);
                    var editorAlias = propNode.Element("EditorAlias").ValueOrDefault(string.Empty);
                    var propertyName = propNode.Element("Name").ValueOrDefault(string.Empty);
                    var sortOrder = propNode.Element("SortOrder").ValueOrDefault(0);

                    logger.Debug<MacroSerializer>(" > Property {0} {1} {2} {3}", propertyAlias, editorAlias, propertyName, sortOrder);

                    var propPath = $"{alias}: {propertyName}";

                    if (item.Properties.ContainsKey(propertyAlias))
                    {
                        logger.Debug<MacroSerializer>(" >> Updating {0}", propertyAlias);
                        item.Properties.UpdateProperty(propertyAlias, propertyName, sortOrder, editorAlias);
                    }
                    else
                    {
                        logger.Debug<MacroSerializer>(" >> Adding {0}", propertyAlias);
                        details.Add(uSyncChange.Create(propPath, "Property", propertyAlias));
                        item.Properties.Add(new MacroProperty(propertyAlias, propertyName, sortOrder, editorAlias));
                    }
                }

            }

            if (options.DeleteItems())
            {
                RemoveOrphanProperties(item, properties);
            }

            return SyncAttempt<IMacro>.Succeed(item.Name, item, ChangeType.Import, details);
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

        protected override SyncAttempt<XElement> SerializeCore(IMacro item, SyncSerializerOptions options)
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
            foreach (var propertyKey in item.Properties.Keys.OrderBy(x => x))
            {
                var property = item.Properties[propertyKey];

                if (property != null)
                {
                    properties.Add(new XElement("Property",
                        new XElement("Name", property.Name),
                        new XElement("Alias", property.Alias),
                        new XElement("SortOrder", property.SortOrder),
                        new XElement("EditorAlias", property.EditorAlias)));
                }
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

        protected override void SaveItem(IMacro item)
            => macroService.Save(item);

        protected override void DeleteItem(IMacro item)
            => macroService.Delete(item);

        protected override string ItemAlias(IMacro item)
            => item.Alias;
    }
}
