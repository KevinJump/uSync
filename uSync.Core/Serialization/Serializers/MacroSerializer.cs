using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("CBB3FDA1-F7B3-470E-B78F-EB316576C8C6", "Macro Serializer", uSyncConstants.Serialization.Macro)]
public class MacroSerializer : SyncSerializerBase<IMacro>, ISyncSerializer<IMacro>
{
    private readonly IMacroService _macroService;
    private readonly IShortStringHelper _shortStringHelper;

    public MacroSerializer(
        IEntityService entityService, ILogger<MacroSerializer> logger,
        IMacroService macroService, IShortStringHelper shortStringHelper)
        : base(entityService, logger)
    {
        this._macroService = macroService;
        this._shortStringHelper = shortStringHelper;
    }

    protected override SyncAttempt<IMacro> DeserializeCore(XElement node, SyncSerializerOptions options)
    {
        var details = new List<uSyncChange>();

        if (node.Element(uSyncConstants.Xml.Name) == null)
            throw new NullReferenceException("XML missing Name parameter");

        var key = node.GetKey();
        var alias = node.GetAlias();
        var name = node.Element(uSyncConstants.Xml.Name).ValueOrDefault(string.Empty);

        var macroSource = node.Element("MacroSource").ValueOrDefault(string.Empty);

        logger.LogDebug("Macro by Key [{key}]", key);
        var item = _macroService.GetById(key);

        if (item == null)
        {
            logger.LogDebug("Macro by Alias [{alias}]", alias);
            item = _macroService.GetByAlias(alias);
        }

        if (item == null)
        {
            logger.LogDebug("Creating New [{alias}]", alias);
            item = new Macro(_shortStringHelper, alias, name, macroSource);
            details.Add(uSyncChange.Create(alias, name, "New Macro"));
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

        if (item.MacroSource != macroSource)
        {
            details.AddUpdate("MacroSource", item.MacroSource, macroSource);
            item.MacroSource = macroSource;
        }

        //if (item.MacroType != macroType)
        //{
        //    details.AddUpdate("MacroType", item.MacroType, macroType);
        //    item.MacroType = macroType;
        //}

        var useInEditor = node.Element("UseInEditor").ValueOrDefault(false);
        var dontRender = node.Element("DontRender").ValueOrDefault(false);
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

                var propertyAlias = propNode.Element(uSyncConstants.Xml.Alias).ValueOrDefault(string.Empty);
                var editorAlias = propNode.Element("EditorAlias").ValueOrDefault(string.Empty);
                var propertyName = propNode.Element(uSyncConstants.Xml.Name).ValueOrDefault(string.Empty);
                var sortOrder = propNode.Element(uSyncConstants.Xml.SortOrder).ValueOrDefault(0);

                logger.LogDebug(" > Property {propertyAlias} {editorAlias} {propertyName} {sortOrder}",
                    propertyAlias, editorAlias, propertyName, sortOrder);

                var propPath = $"{alias}: {propertyName}";

                if (item.Properties.ContainsKey(propertyAlias))
                {
                    logger.LogDebug(" >> Updating {propertyAlias}", propertyAlias);
                    item.Properties.UpdateProperty(propertyAlias, propertyName, sortOrder, editorAlias);
                }
                else
                {
                    logger.LogDebug(" >> Adding {propertyAlias}", propertyAlias);
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
                .Where(x => x.Element(uSyncConstants.Xml.Alias).ValueOrDefault(string.Empty) != string.Empty)
                .Select(x => x.Element(uSyncConstants.Xml.Alias).ValueOrDefault(string.Empty))
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

        node.Add(new XElement(uSyncConstants.Xml.Name, item.Name));
        node.Add(new XElement("MacroSource", item.MacroSource));
        // node.Add(new XElement("MacroType", item.MacroType));
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
                    new XElement(uSyncConstants.Xml.Name, property.Name),
                    new XElement(uSyncConstants.Xml.Alias, property.Alias),
                    new XElement(uSyncConstants.Xml.SortOrder, property.SortOrder),
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

    public override IMacro FindItem(int id)
        => _macroService.GetById(id);

    public override IMacro FindItem(Guid key)
        => _macroService.GetById(key);

    public override IMacro FindItem(string alias)
        => _macroService.GetByAlias(alias);

    public override void SaveItem(IMacro item)
        => _macroService.Save(item);

    public override void DeleteItem(IMacro item)
        => _macroService.Delete(item);

    public override string ItemAlias(IMacro item)
        => item.Alias;
}
