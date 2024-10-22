using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Extensions;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

public abstract class ContentTypeBaseSerializer<TObject> : SyncContainerSerializerBase<TObject>
    where TObject : IContentTypeComposition
{
    private readonly IDataTypeService _dataTypeService;
    private readonly IContentTypeBaseService<TObject> _baseService;
    protected readonly IShortStringHelper shortStringHelper;

    protected readonly IAppCache _appCache;

    protected List<string>? aliasCache { get; set; }

    protected ContentTypeBaseSerializer(
        IEntityService entityService,
        IEntityTypeContainerService<TObject>? entityTypeContainerService,
        ILogger<ContentTypeBaseSerializer<TObject>> logger,
        IDataTypeService dataTypeService,
        IContentTypeBaseService<TObject> baseService,
        UmbracoObjectTypes containerType,
        IShortStringHelper shortStringHelper,
        AppCaches appCaches)
        : base(entityService, entityTypeContainerService, logger, containerType)
    {
        _dataTypeService = dataTypeService;
        _baseService = baseService;
        this.shortStringHelper = shortStringHelper;

        _appCache = appCaches.RuntimeCache;
    }

    #region Serialization 

    protected XElement SerializeBase(TObject item)
    {
        return InitializeBaseNode(item, item.Alias, item.Level);
    }

    protected XElement SerializeInfo(TObject item)
    {
        return new XElement(uSyncConstants.Xml.Info,
                        new XElement("Name", item.Name),
                        new XElement("Icon", item.Icon),
                        new XElement("Thumbnail", item.Thumbnail),
                        new XElement("Description", string.IsNullOrWhiteSpace(item.Description) ? "" : item.Description),
                        new XElement("AllowAtRoot", item.AllowedAsRoot.ToString()),
                        // new XElement("IsListView", item.IsContainer.ToString()),
                        new XElement("ListView", item.ListView ?? Guid.Empty),
                        new XElement("Variations", item.Variations),
                        new XElement("IsElement", item.IsElement)); ;
    }

    protected XElement SerializeTabs(TObject item)
    {
        var tabs = new XElement("Tabs");

        foreach (var tab in item.PropertyGroups.OrderBy(x => x.Key))
        {
            tabs.Add(new XElement("Tab",
                        new XElement(uSyncConstants.Xml.Key, tab.Key),
                        new XElement("Caption", tab.Name),
                        new XElement("Alias", tab.Alias),
                        new XElement("Type", tab.Type),
                        new XElement("SortOrder", tab.SortOrder)));

        }

        return tabs;
    }

    protected virtual XElement SerializeProperties(TObject item)
    {
        var node = new XElement("GenericProperties");

        foreach (var property in item.PropertyTypes.OrderBy(x => x.Alias))
        {
            var propNode = new XElement("GenericProperty",
                new XElement(uSyncConstants.Xml.Key, property.Key),
                new XElement(uSyncConstants.Xml.Name, property.Name),
                new XElement(uSyncConstants.Xml.Alias, property.Alias));

            var def = _dataTypeService.GetAsync(property.DataTypeKey).Result;
            // var def = _dataTypeService.GetDataType(property.DataTypeId);
            if (def != null)
            {
                propNode.Add(new XElement("Definition", def.Key));
                propNode.Add(new XElement("Type", def.EditorAlias));
            }
            else
            {
                propNode.Add(new XElement("Type", property.PropertyEditorAlias));
            }

            propNode.Add(new XElement("Mandatory", property.Mandatory));
            propNode.Add(new XElement("Validation",
                string.IsNullOrEmpty(property.ValidationRegExp) ? "" : property.ValidationRegExp));

            var description = string.IsNullOrEmpty(property.Description) ? "" : property.Description;
            propNode.Add(new XElement("Description", new XCData(description)));

            propNode.Add(new XElement("SortOrder", property.SortOrder));

            // cross version compatibility, before v8.17 - tabs are by name. 
            var tab = item.PropertyGroups.FirstOrDefault(x => x.PropertyTypes?.Contains(property) is true);
            var tabNode = new XElement("Tab", tab != null ? tab.Name : "");
            if (tab != null) tabNode.Add(new XAttribute("Alias", tab.Alias));
            propNode.Add(tabNode);

            SerializeExtraProperties(propNode, item, property);

            propNode.Add(new XElement("MandatoryMessage",
                string.IsNullOrEmpty(property.MandatoryMessage) ? "" : property.MandatoryMessage));

            propNode.Add(new XElement("ValidationRegExpMessage",
                string.IsNullOrEmpty(property.ValidationRegExpMessage) ? "" : property.ValidationRegExpMessage));

            propNode.Add(new XElement("LabelOnTop", property.LabelOnTop));

            node.Add(propNode);
        }

        return node;
    }

    /// <summary>
    ///  Serialize properties that have been introduced in later versions of umbraco.
    /// </summary>
    /// <remarks>
    ///  by doing this like this it makes us keep our backwards compatibility, while also supporting 
    ///  newer properties. 
    /// </remarks>
    protected void SerializeNewProperty<TValue>(XElement node, IPropertyType property, string propertyName)
    {
        var propertyInfo = property?.GetType()?.GetProperty(propertyName);
        if (propertyInfo != null)
        {
            var value = propertyInfo.GetValue(property);

            var attempt = value.TryConvertTo<TValue>();
            if (attempt.Success)
            {
                if (attempt.Result != null)
                {
                    node.Add(new XElement(propertyName, attempt.Result));
                }
                else
                {
                    node.Add(new XElement(propertyName, string.Empty));
                }
            }
        }
    }


    protected virtual void SerializeExtraProperties(XElement node, TObject item, IPropertyType property)
    {
        // when something has extra properties that the others don't (memberTypes at the moment)
    }


    protected XElement SerializeStructure(TObject item)
        => SerializeStructureAsync(item).Result;

    protected async Task<XElement> SerializeStructureAsync(TObject item)
    {
        var node = new XElement("Structure");

        var allowedTypeOrdered = item.AllowedContentTypes?.OrderBy(x => x.SortOrder);
        if (allowedTypeOrdered is null) return node;

        foreach (var allowedType in allowedTypeOrdered)
        {
            var allowedItem = await FindItemAsync(allowedType.Key);
            if (allowedItem != null)
            {
                node.Add(new XElement(ItemType,
                    new XAttribute(uSyncConstants.Xml.Key, allowedItem.Key),
                    new XAttribute(uSyncConstants.Xml.SortOrder, allowedType.SortOrder), allowedItem.Alias));
            }
        }
        return node;
    }

    protected XElement SerializeCompositions(ContentTypeCompositionBase item)
    {
        var compNode = new XElement("Compositions");
        var compositions = item.ContentTypeComposition;
        foreach (var composition in compositions.OrderBy(x => x.Alias))
        {
            compNode.Add(new XElement("Composition", composition.Alias,
                new XAttribute(uSyncConstants.Xml.Key, composition.Key)));
        }

        return compNode;
    }

    #endregion

    #region De-serialization


    protected async Task<IEnumerable<uSyncChange>> DeserializeBaseAsync(TObject item, XElement node)
    {
        logger.LogDebug("De-serializing Base");

        if (node == null) return [];

        var info = node.Element(uSyncConstants.Xml.Info);
        if (info is null) return [];

        List<uSyncChange> changes = [];

        var key = node.GetKey();
        if (item.Key != key)
        {
            changes.AddUpdate(uSyncConstants.Xml.Key, item.Key, key, "");
            item.Key = key;
        }


        var alias = SetSafeAliasValue(item, node, true);

        var name = info.Element(uSyncConstants.Xml.Name).ValueOrDefault(string.Empty);
        if (!string.IsNullOrEmpty(name) && item.Name != name)
        {
            changes.AddUpdate(uSyncConstants.Xml.Name, item.Name ?? "(None)", name, "");
            item.Name = name;
        }

        var icon = info.Element("Icon").ValueOrDefault(string.Empty);
        if (item.Icon != icon)
        {
            changes.AddUpdate("Icon", item.Icon ?? "(None)", icon, "");
            item.Icon = icon;
        }

        var thumbnail = info.Element("Thumbnail").ValueOrDefault(string.Empty);
        if (item.Thumbnail != thumbnail)
        {
            changes.AddUpdate("Icon", item.Thumbnail ?? "(None)", thumbnail, "");
            item.Thumbnail = thumbnail;
        }

        var description = info.Element("Description").ValueOrDefault(string.Empty);
        if (item.Description != description)
        {
            changes.AddUpdate("Description", item.Description ?? "(None)", description, "");
            item.Description = description;
        }

        var allowedAsRoot = info.Element("AllowAtRoot").ValueOrDefault(false);
        if (item.AllowedAsRoot != allowedAsRoot)
        {
            changes.AddUpdate("AllowAtRoot", item.AllowedAsRoot, allowedAsRoot, "");
            item.AllowedAsRoot = allowedAsRoot;
        }

        var variations = info.Element("Variations").ValueOrDefault(ContentVariation.Nothing);
        if (item.Variations != variations)
        {
            changes.AddUpdate("Variations", item.Variations, variations, "");
            item.Variations = variations;
        }

        var isElement = info.Element("IsElement").ValueOrDefault(false);
        if (item.IsElement != isElement)
        {
            changes.AddUpdate("IsElement", item.IsElement, isElement, "");
            item.IsElement = isElement;
        }

        //var isContainer = info.Element("IsListView").ValueOrDefault(false);
        //if (item.IsContainer != isContainer)
        //{
        //    changes.AddUpdate("IsListView", item.IsContainer, isContainer, "");
        //    item.IsContainer = isContainer;
        //}

        var listView = info.Element("ListView").ValueOrDefault(Guid.Empty);
        if (listView != Guid.Empty && item.ListView != listView)
        {
            item.ListView = listView;
        }

        if (!SetMasterFromElement(item, info.Element(uSyncConstants.Xml.Parent)))
        {
            await SetFolderFromElementAsync(item, info.Element("Folder"));
        }

        return changes;
    }

    protected SyncAttempt<TObject> DeserializedResult(TObject item, List<uSyncChange> details, SyncSerializerOptions options)
    {
        var message = "";

        if (details.HasWarning())
        {
            if (options.FailOnWarnings())
            {
                // Fail on warning. means we don't save or publish because something is wrong ?
                return SyncAttempt<TObject>.Fail(item.Name ?? item.Alias, item, ChangeType.ImportFail, "Failed with warnings", details,
                    new Exception("Import failed because of warnings, and fail on warnings is true"));
            }
            else
            {
                message = "Imported with warnings";
            }
        }

        return SyncAttempt<TObject>.Succeed(item.Name ?? item.Alias, item, ChangeType.Import, message, false, details);

    }


    [Obsolete("Use DeserializeBaseAsync will be removed in v16")]
    protected IEnumerable<uSyncChange> DeserializeStructure(TObject item, XElement node)
        => DeserializeStructureAsync(item, node).Result;

    protected async Task<IEnumerable<uSyncChange>> DeserializeStructureAsync(TObject item, XElement node)
    {
        logger.LogDebug("De-serializing Structure");

        var structure = node.Element("Structure");
        if (structure == null) return [];

        List<uSyncChange> changes = [];
        List<ContentTypeSort> allowed = [];

        int sortOrder = 0;

        foreach (var baseNode in structure.Elements(ItemType))
        {
            logger.LogDebug("baseNode {base}", baseNode.ToString());
            var alias = baseNode.Value;
            var key = baseNode.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);

            logger.LogDebug("Structure: {key}", key);

            var itemSortOrder = baseNode.Attribute(uSyncConstants.Xml.SortOrder).ValueOrDefault(sortOrder);
            logger.LogDebug("Sort Order: {sortOrder}", itemSortOrder);

            IContentTypeBase? baseItem = default;

            if (key != Guid.Empty)
            {
                logger.LogDebug("Structure By Key {key}", key);
                // lookup by key (our preferred way)
                baseItem = await FindItemAsync(key);
            }

            if (baseItem == null)
            {
                logger.LogDebug("Structure By Alias: {alias}", alias);
                // lookup by alias (less nice)
                baseItem = await FindItemAsync(alias);
            }

            if (baseItem != null)
            {
                logger.LogDebug("Structure Found {alias}", baseItem.Alias);
                allowed.Add(new ContentTypeSort(baseItem.Key, itemSortOrder, baseItem.Alias));
                sortOrder = itemSortOrder + 1;
            }
        }

        logger.LogDebug("Structure: {count} items", allowed.Count);


        if (item.AllowedContentTypes is not null)
        {
            // compare the two lists (the equality compare fails because the id value is lazy)
            var currentHash = string.Join(":", item.AllowedContentTypes.Select(x => $"{x.Key}-{x.SortOrder}").OrderBy(x => x));
            var newHash = string.Join(":", allowed.Select(x => $"{x.Key}-{x.SortOrder}").OrderBy(x => x));

            if (!currentHash.Equals(newHash))
            {
                changes.AddUpdate("Allowed",
                    string.Join(",", item.AllowedContentTypes.Select(x => x.Alias) ?? []),
                    string.Join(",", allowed.Select(x => x.Alias) ?? []), "/Structure");

                logger.LogDebug("Updating allowed content types {old}, {new}", currentHash, newHash);
                item.AllowedContentTypes = allowed;
            }
        }
        else
        {
            item.AllowedContentTypes = allowed;
        }

        return changes;
    }

    protected IEnumerable<uSyncChange> DeserializeProperties(TObject item, XElement node, SyncSerializerOptions options)
    {
        logger.LogDebug("De-serializing Properties");

        var propertiesNode = node?.Element("GenericProperties");
        if (propertiesNode == null) return [];

        /// there are something we can't do in the loop, 
        /// so we store them and do them once we've put 
        /// things in. 
        List<uSyncChange> changes = [];
        Dictionary<string, string> propertiesToMove = [];

        List<string>? compositeProperties = default;

        foreach (var propertyNode in propertiesNode.Elements("GenericProperty"))
        {
            var alias = propertyNode.Element(uSyncConstants.Xml.Alias).ValueOrDefault(string.Empty);
            if (string.IsNullOrEmpty(alias)) continue;

            var key = propertyNode.Element(uSyncConstants.Xml.Key).ValueOrDefault(alias.GetHashCode().ToGuid());
            var definitionKey = propertyNode.Element("Definition").ValueOrDefault(Guid.Empty);
            var propertyEditorAlias = propertyNode.Element("Type").ValueOrDefault(string.Empty);

            logger.LogDebug(" > Property: {alias} {key} {definitionKey} {editorAlias}", alias, key, definitionKey, propertyEditorAlias);

            var property = GetOrCreateProperty(item, key, alias, definitionKey, propertyEditorAlias, compositeProperties, out bool IsNew);
            if (property == null)
            {
                changes.AddWarning($"Property/{alias}", alias, $"Property '{alias}' cannot be created (missing DataType?)");
                continue;
            }

            if (key != Guid.Empty && property.Key != key)
            {
                changes.AddUpdate(uSyncConstants.Xml.Key, property.Key, key, $"{alias}/Key");
                property.Key = key;
            }

            if (property.Alias != alias)
            {
                changes.AddUpdate(uSyncConstants.Xml.Alias, property.Alias, alias, $"{alias}/Alias");
                property.Alias = alias;
            }

            var name = propertyNode.Element(uSyncConstants.Xml.Name).ValueOrDefault(alias);
            if (property.Name != name)
            {
                changes.AddUpdate(uSyncConstants.Xml.Name, property.Name, name, $"{alias}/Name");
                property.Name = name;
            }

            var description = propertyNode.Element("Description").ValueOrDefault(string.Empty);
            if (property.Description != description)
            {
                changes.AddUpdate("Description", property.Description ?? "(None)", description, $"{alias}/Description");
                property.Description = description;
            }

            var mandatory = propertyNode.Element("Mandatory").ValueOrDefault(false);
            if (property.Mandatory != mandatory)
            {
                changes.AddUpdate("Mandatory", property.Mandatory, mandatory, $"{alias}/Mandatory");
                property.Mandatory = mandatory;
            }

            var regEx = propertyNode.Element("Validation").ValueOrDefault(string.Empty);
            if (property.ValidationRegExp != regEx)
            {
                changes.AddUpdate("Validation", property.ValidationRegExp ?? "(None)", regEx, $"{alias}/RegEx");
                property.ValidationRegExp = propertyNode.Element("Validation").ValueOrDefault(string.Empty);
            }

            var sortOrder = propertyNode.Element("SortOrder").ValueOrDefault(0);
            if (property.SortOrder != sortOrder)
            {
                changes.AddUpdate("SortOrder", property.SortOrder, sortOrder, $"{alias}/SortOrder");
                property.SortOrder = sortOrder;
            }

            var mandatoryMessage = propertyNode.Element("MandatoryMessage").ValueOrDefault(string.Empty);
            if (property.MandatoryMessage != mandatoryMessage)
            {
                changes.AddUpdate("MandatoryMessage", property.MandatoryMessage ?? "(None)", mandatoryMessage, $"{alias}/MandatoryMessage");
                property.MandatoryMessage = mandatoryMessage;
            }

            var validationRegExMessage = propertyNode.Element("ValidationRegExpMessage").ValueOrDefault(string.Empty);
            if (property.ValidationRegExpMessage != validationRegExMessage)
            {
                changes.AddUpdate("ValidationRegExpMessage", property.ValidationRegExpMessage ?? "(None)", validationRegExMessage, $"{alias}/ValidationRegExpMessage");
                property.ValidationRegExpMessage = validationRegExMessage;
            }

            var labelOnTop = propertyNode.Element("LabelOnTop").ValueOrDefault(false);
            if (property.LabelOnTop != labelOnTop)
            {
                changes.AddUpdate("LabelOnTop", property.LabelOnTop, labelOnTop, $"{alias}/LabelOnTop");
                property.LabelOnTop = labelOnTop;
            }

            changes.AddRange(DeserializeExtraProperties(item, property, propertyNode));

            var tabAlias = GetTabAlias(item, propertyNode.Element("Tab"));

            if (IsNew)
            {
                changes.AddNew(alias, name, alias);
                logger.LogDebug("Property {alias} is new adding to tab. {tabAlias}", alias, tabAlias ?? "(No tab name)");

                if (string.IsNullOrWhiteSpace(tabAlias))
                {

                    item.AddPropertyType(property);
                }
                else
                {
                    var tabGroup = item.PropertyGroups.FindTab(tabAlias);
                    if (tabGroup == null)
                    {
                        logger.LogWarning("Unable to find tab {tabAlias} it doesn't seem to exist on the content type", tabAlias);
                        changes.AddWarning(alias, name, $"Unable to find tab {tabAlias} to add property too");
                    }
                    else
                    {
                        item.AddPropertyType(property, tabGroup?.Alias ?? tabAlias);
                    }
                }
            }
            else
            {
                logger.LogDebug("Property {alias} exists, checking tab location {tabAlias}", alias, tabAlias);
                // we need to see if this one has moved. 
                if (!string.IsNullOrWhiteSpace(tabAlias))
                {
                    var tabGroup = item.PropertyGroups.FindTab(tabAlias);
                    if (tabGroup != null)
                    {
                        if (!tabGroup.PropertyTypes?.Contains(property.Alias) is true)
                        {
                            // this property is not currently in this tab.
                            // add to our move list.
                            propertiesToMove[property.Alias] = tabAlias;
                        }
                    }
                    else
                    {
                        logger.LogWarning("Cannot find tab {alias} to add {property} to", tabAlias, property.Alias);
                        changes.AddWarning(alias, name, $"Unable to find tab {tabAlias} to add property too");
                    }
                }
            }
        }

        // move things between tabs. 
        changes.AddRange(ContentTypeBaseSerializer<TObject>.MoveProperties(item, propertiesToMove));

        if (options.DeleteItems())
        {
            // remove what needs to be removed
            changes.AddRange(RemoveProperties(item, propertiesNode));
        }
        else
        {
            logger.LogDebug("Property Removal disabled by configuration");
        }

        return changes;

    }

    /// <summary>
    ///  Get the alias for the tab (assuming it exists).
    /// </summary>
    /// <remarks>
    ///  gives us some backwards compatibility with 8.17 or older sites, which didn't have tabs.
    /// </remarks>
    private string GetTabAlias(TObject item, XElement? tabNode)
    {
        if (tabNode == null) return string.Empty;

        var alias = tabNode.Attribute(uSyncConstants.Xml.Alias).ValueOrDefault(string.Empty);
        if (!string.IsNullOrWhiteSpace(alias)) return alias;

        var name = tabNode.ValueOrDefault(string.Empty);
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        // else we only have the name, we need to go find the alias. 
        var tab = item.PropertyGroups.FirstOrDefault(x => x.Name.InvariantEquals(name));
        if (tab != null) return tab.Alias;

        // if all else fails we return the alias to the name, 
        // this will create a group with this name - which is the default behavior.
        return name.ToSafeAlias(shortStringHelper, true);
    }


    /// <summary>
    ///  sets the alias to a 'safe' value so it there is a clash 
    ///  (because of a rename) it will still work
    /// </summary>
    /// <param name="item"></param>
    /// <param name="node"></param>
    protected string SetSafeAliasValue(TObject item, XElement node, bool ensureSafeValue)
    {
        var nodeAlias = node.GetAlias();
        if (item.Alias != nodeAlias)
        {
            var oldAlias = item.Alias;

            var safeAlias = ensureSafeValue ? GetSafeItemAlias(nodeAlias) : nodeAlias;

            // only set the item if its not already the value
            if (item.Alias != safeAlias)
                item.Alias = safeAlias;

            // add the alias to the cache
            AddAlias(safeAlias);

            // remove the previous alias from our cache. 
            RemoveAlias(oldAlias);

            return safeAlias;
        }

        return nodeAlias;
    }


    /// <summary>
    ///  method checks that the alias we want to set something too doesn't already exist. 
    /// </summary>
    /// <param name="alias"></param>
    /// <returns></returns>
    protected string GetSafeItemAlias(string alias)
    {
        // if its null we get a new list
        EnsureAliasCache();

        if (aliasCache?.Contains(alias) is true)
        {
            logger.LogDebug("Alias clash {alias} already exists", alias);
            return $"{alias}_{Guid.NewGuid().ToShortKeyString(8)}";
        }

        return alias;
    }

    protected virtual void EnsureAliasCache() { }
    

    protected void ClearAliases()
    {
        aliasCache = null;
        _appCache.ClearByKey($"usync_{this.Id}");
    }

    protected void RemoveAlias(string alias)
    {
        if (aliasCache != null && aliasCache.Contains(alias))
            aliasCache.Remove(alias);

        RefreshAliasCache();

        logger.LogDebug("remove [{alias}] - {cache}", alias,
            aliasCache != null ? string.Join(",", aliasCache) : "Empty");
    }

    private void RefreshAliasCache()
    {
        _appCache.ClearByKey($"usync_{this.Id}");
        _appCache.GetCacheItem($"usync_{this.Id}", () => { return aliasCache; });
    }

    protected void AddAlias(string alias)
    {
        EnsureAliasCache();

        if (!aliasCache?.Contains(alias) is true)
            aliasCache?.Add(alias);

        RefreshAliasCache();
        logger.LogDebug("Add [{aliaS}] - {cache}", alias, string.Join(",", aliasCache ?? []));
    }



    /// <summary>
    ///  De-serialize properties added in later versions of Umbraco.
    /// </summary>
    /// <remarks>
    ///  using reflection to find properties that might have been added in later versions of umbraco.
    ///  doing it this way means we can maintain backwards compatibility.
    /// </remarks>
    protected uSyncChange? DeserializeNewProperty<TValue>(IPropertyType property, XElement node, string propertyName)
    {
        var propertyInfo = property?.GetType()?.GetProperty(propertyName);
        if (propertyInfo != null)
        {
            var value = node.Element(propertyName).ValueOrDefault(string.Empty);
            var attempt = value.TryConvertTo<TValue>();
            if (attempt.Success)
            {
                var current = ContentTypeBaseSerializer<TObject>.GetPropertyAs<TValue>(propertyInfo, property);

                if (current == null || !current.Equals(attempt.Result))
                {
                    propertyInfo.SetValue(property, attempt.Result);

                    return uSyncChange.Update($"property/{propertyName}",
                        propertyName,
                        current?.ToString() ?? "(Blank)",
                        attempt.Result?.ToString());
                }
            }
        }

        return null;
    }

    private static TValue? GetPropertyAs<TValue>(PropertyInfo info, IPropertyType? property)
    {
        if (info == null) return default;

        var value = info.GetValue(property);
        if (value == null) return default;

        var result = value.TryConvertTo<TValue>();
        if (result.Success)
            return result.Result;

        return default;

    }


    virtual protected IEnumerable<uSyncChange> DeserializeExtraProperties(TObject item, IPropertyType property, XElement node)
    {
        return [];
    }

    private class TabInfo
    {
        public TabInfo(string alias)
        {
            Alias = alias;
        }

        public string? Name { get; set; }
        public string Alias { get; set; }

        public int SortOrder { get; set; }
        public PropertyGroupType Type { get; set; }
        public Guid Key { get; set; }
        public int Depth { get; set; }
    }

    private static List<TabInfo>? LoadTabInfo(XElement node)
    {
        var tabNode = node.Element("Tabs");
        if (tabNode == null) return null;

        var tabs = new List<TabInfo>();

        var defaultSort = 0;
        var defaultType = ContentTypeBaseSerializer<TObject>.GetDefaultTabType(tabNode);

        foreach (var tab in tabNode.Elements("Tab"))
        {
            var tabInfo = new TabInfo(tab.Element(uSyncConstants.Xml.Alias).ValueOrDefault(string.Empty))
            {
                Name = tab.Element("Caption").ValueOrDefault(string.Empty),
                SortOrder = tab.Element(uSyncConstants.Xml.SortOrder).ValueOrDefault(defaultSort),
                Type = tab.Element("Type").ValueOrDefault(defaultType),
                Key = tab.Element(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty)
            };

            tabInfo.Depth = tabInfo.Alias.Count(x => x == '/');

            tabs.Add(tabInfo);

            defaultSort = tabInfo.SortOrder + 1;
        }

        return tabs;
    }

    protected IEnumerable<uSyncChange> DeserializeTabs(TObject item, XElement node)
    {
        logger.LogDebug("De-serializing Tabs");

        bool supportsPublishing = item is ContentType;

        var tabs = ContentTypeBaseSerializer<TObject>.LoadTabInfo(node);
        if (tabs is null) return [];

        var changes = new List<uSyncChange>();

        foreach (var tab in tabs.OrderBy(x => x.Depth))
        {
            logger.LogDebug("> Tab {name} {alias} {sortOrder} {depth}", tab.Name, tab.Alias, tab.SortOrder, tab.Depth);

            var existing = ContentTypeBaseSerializer<TObject>.FindTab(item, tab.Alias, tab.Name, tab.Key);
            if (existing != null)
            {
                if (existing.Alias != tab.Alias)
                {
                    changes.AddUpdate(uSyncConstants.Xml.Alias, existing.Alias, tab.Alias, $"Tabs/{tab.Name}/Alias");
                    existing.Alias = tab.Alias;
                }

                if (existing.Name != tab.Name)
                {
                    changes.AddUpdate(uSyncConstants.Xml.Name, existing.Name ?? "(None)", tab.Name ?? "(None)", $"Tabs/{tab.Name}/Name");
                    existing.Name = tab.Name;
                }

                if (tab.Key != Guid.Empty && existing.Key != tab.Key)
                {
                    changes.AddUpdate(uSyncConstants.Xml.Key, existing.Key.ToString(), tab.Key.ToString(), $"Tabs/{tab.Name}/Key");
                    existing.Key = tab.Key;
                }

                if (existing.SortOrder != tab.SortOrder)
                {
                    changes.AddUpdate(uSyncConstants.Xml.SortOrder, existing.SortOrder, tab.SortOrder, $"Tabs/{tab.Name}/SortOrder");
                    existing.SortOrder = tab.SortOrder;
                }

                if (existing.Type != tab.Type)
                {
                    // check for clash. 
                    if (TabClashesWithExisting(item, tab.Alias, tab.Type))
                    {
                        existing.Alias = SyncPropertyGroupHelpers.GetTempTabAlias(tab.Alias);
                    }

                    changes.AddUpdate("Tab type", existing.Type, tab.Type, $"Tabs/{tab.Name}/Type");
                    existing.Type = tab.Type;
                }
            }
            else
            {
                // if the alias is blank, we make it up.
                if (string.IsNullOrWhiteSpace(tab.Alias))
                    tab.Alias = tab.Name?.ToSafeAlias(shortStringHelper, true) ?? "Unknown Tab";

                // if the alias & type would clash with something already in the tree
                // *e.g this is a group with `name` when tab with `name` already being used
                // by ancestor or descendant.
                if (TabClashesWithExisting(item, tab.Alias, tab.Type))
                    tab.Alias = SyncPropertyGroupHelpers.GetTempTabAlias(tab.Alias);

                // v14: if the tab is a child (group) - then we might need to create a parent tab
                if (tab.Depth > 0)
                {
                    var tabRoot = tab.Alias.Split('/')[0];
                    if (item.PropertyGroups.Contains(tabRoot))
                    {
                        logger.LogDebug("Parent Tab {tabRoot} already exists", tabRoot);
                    }
                    else
                    {
                        logger.LogDebug("Parent Tab {tabRoot} doesn't exist, creating", tabRoot);

                        var compositionParent = item.CompositionPropertyGroups.FirstOrDefault(x => x.Alias.InvariantEquals(tabRoot));
                        if (compositionParent != null)
                        {
                            logger.LogInformation("Creating parent tab from inherited tab data, {alias} {name}", compositionParent.Alias, compositionParent.Name ?? compositionParent.Alias); 

                            item.AddPropertyGroup(tabRoot, compositionParent.Name ?? tabRoot);
                            item.PropertyGroups[tabRoot].Type = compositionParent.Type;
                            item.PropertyGroups[tabRoot].SortOrder = compositionParent.SortOrder;
                        }
                    }
                }

                // create the tab
                item.AddPropertyGroup(tab.Alias, tab.Name ?? tab.Alias);
                var propertyGroup = item.PropertyGroups[tab.Alias];

                changes.AddNew(tab.Name ?? tab.Alias, tab.Name ?? tab.Alias, $"Tabs/{tab.Name}");
                var newTab = item.PropertyGroups.FirstOrDefault(x => x.Alias.InvariantEquals(tab.Alias));
                if (newTab is not null)
                {
                    newTab.SortOrder = tab.SortOrder;
                    newTab.Type = tab.Type;
                    if (tab.Key != Guid.Empty) newTab.Key = tab.Key;
                }
            }
        }

        ClearAllTabsCache();

        return changes;
    }

    private static PropertyGroup? FindTab(TObject item, string alias, string? name, Guid key)
    {
        if (key != Guid.Empty)
        {
            var tab = item.PropertyGroups.FirstOrDefault(x => x.Key == key);
            if (tab != null) return tab;
        }

        if (string.IsNullOrWhiteSpace(alias))
            return item.PropertyGroups.FirstOrDefault(x => x.Name.InvariantEquals(name));

        return item.PropertyGroups.FirstOrDefault(x => x.Alias.InvariantContains(alias));
    }

    /// <summary>
    ///  calculate what the default type of group is. 
    /// </summary>
    /// <remarks>
    ///  this shouldn't be an issue, but when we import legacy items we don't know what type
    ///  of thing the group it (tab or group). 
    ///  
    ///  by default they are probably groups, but if the DocType already has tabs they need
    ///  to be treated as new tabs or they don't appear.
    /// </remarks>
    private static PropertyGroupType GetDefaultTabType(XElement node)
    {
        // if we have tabs then when we don't know the type of a group, its a tab.
        if (node.Elements("Tab").Any(x => x.Element("Type").ValueOrDefault(PropertyGroupType.Group) == PropertyGroupType.Tab))
            return PropertyGroupType.Tab;

        // if we don't have tabs then all unknown types are groups.
        return PropertyGroupType.Group;
    }


    /// <summary>
    ///  Returns either the alias or an alias made from the name of the tab. 
    /// </summary>
    private string GetTabAliasFromTabGroup(XElement tabNode)
    {
        if (tabNode == null) return string.Empty;

        var alias = tabNode.Element(uSyncConstants.Xml.Alias).ValueOrDefault(string.Empty);
        if (!string.IsNullOrWhiteSpace(alias)) return alias;

        var name = tabNode.Element("Caption").ValueOrDefault(string.Empty);
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        return name.ToSafeAlias(shortStringHelper, true);
    }

    /// <summary>
    ///  remove any prefixes we may have added to a tab alias
    /// </summary>
    /// <param name="item"></param>
    protected void CleanTabAliases(TObject item)
    {
        foreach (var tab in item.PropertyGroups)
        {
            if (SyncPropertyGroupHelpers.IsTempTabAlias(tab.Alias))
            {
                tab.Alias = SyncPropertyGroupHelpers.StripTempTabAlias(tab.Alias);
            }
        }
    }

    protected IEnumerable<uSyncChange> CleanTabs(TObject item, XElement node, SyncSerializerOptions options)
    {
        if (options.DeleteItems())
        {
            var tabNode = node?.Element("Tabs");
            if (tabNode == null) return [];

            var newTabs = tabNode.Elements("Tab")
                .Select(x => GetTabAliasFromTabGroup(x))
                .ToList();

            var inheritedTabs =item.CompositionPropertyGroups.Select(x => x.Alias).ToList();

            List<PropertyGroup> removals = [];
            foreach (var tab in item.PropertyGroups)
            {
                // don't remove the inherited tabs
                if (inheritedTabs.Contains(tab.Alias)) continue;

                if (!newTabs.InvariantContains(tab.Alias))
                {
                    removals.Add(tab);
                }
            }

            if (removals.Count > 0)
            {
                var changes = new List<uSyncChange>();

                foreach (var tab in removals)
                {
                    if (tab.PropertyTypes?.Count > 0)
                    {
                        logger.LogWarning("Not removing {tab} as it still has properties {properties}", tab.Alias,
                            String.Join(",", tab.PropertyTypes.Select(x => x.Name)));
                    }
                    else
                    {
                        logger.LogInformation("Removing tab : {alias}", tab.Alias);
                        changes.Add(uSyncChange.Delete($"Tabs/{tab.Alias}", tab.Alias, tab.Alias));
                        item.PropertyGroups.Remove(tab);
                    }
                }

                return changes;
            }
        }

        return [];
    }

    [Obsolete("Use CleanFolderAsync will be removed in v16")]
    protected void CleanFolder(TObject item, XElement node)
        => CleanFolderAsync(item, node).Wait();
    protected async Task CleanFolderAsync(TObject item, XElement node) 
    {
        var folderNode = node.Element(uSyncConstants.Xml.Info)?.Element("Folder");
        if (folderNode is null) return;

        var key = folderNode.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);
        if (key == Guid.Empty) return;


        logger.LogDebug("Folder Key {key}", key.ToString());

        var folder = await FindContainerAsync(key);
        if (folder is not null) return;

        logger.LogDebug("Clean folder - Key doesn't not match");
        await FindFolderAsync(key, folderNode.Value);
    }

    [Obsolete("Use CleanMasterAsync will be removed in v16")]
    protected IEnumerable<uSyncChange> DeserializeCompositions(TObject item, XElement node)
        => DeserializeCompositionsAsync(item, node).Result;
    
    protected async Task<IEnumerable<uSyncChange>> DeserializeCompositionsAsync(TObject item, XElement node)
    {
        logger.LogDebug("{alias} De-serializing Compositions", item.Alias);

        var comps = node?.Element(uSyncConstants.Xml.Info)?.Element("Compositions");
        if (comps == null) return [];

        List<IContentTypeComposition> compositions = [];

        foreach (var compositionNode in comps.Elements("Composition"))
        {
            var alias = compositionNode.Value;
            var key = compositionNode.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);

            logger.LogDebug("{itemAlias} > Comp {alias} {key}", item.Alias, alias, key);

            var type = await FindItemAsync(key, alias);
            if (type != null)
                compositions.Add(type);
        }

        // compare (but sorted by key) only make the change if it is different.
        if (!Enumerable.SequenceEqual(item.ContentTypeComposition.OrderBy(x => x.Key), compositions.OrderBy(x => x.Key)))
        {
            var change = uSyncChange.Update(uSyncConstants.Xml.Info, "Compositions",
                string.Join(",", item.ContentTypeComposition.Select(x => x.Alias)),
                string.Join(",", compositions.Select(x => x.Alias)));

            item.ContentTypeComposition = compositions;

            return change.AsEnumerableOfOne();
        }

        return [];
    }


    private async Task SetFolderFromElementAsync(IContentTypeBase item, XElement? folderNode)
    {
        if (folderNode is null) return;

        var folder = folderNode.ValueOrDefault(string.Empty);
        if (string.IsNullOrWhiteSpace(folder))
        {
            if (item.ParentId != Constants.System.Root)
            {
                item.ParentId = Constants.System.Root;
            }
        }
        else
        {
            var container = await FindFolderAsync(folderNode.GetKey(), folder);
            if (container != null && container.Id != item.ParentId)
            {
                item.SetParent(container);
            }
        }
    }


    private bool SetMasterFromElement(IContentTypeBase item, XElement? masterNode)
    {
        if (masterNode is null) return false;

        var key = masterNode.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);
        if (key != Guid.Empty)
        {
            var entity = entityService.Get(key);
            if (entity != null)
            {
                if (entity.Id != item.ParentId)
                {
                    item.SetParent(entity);
                }
                return true;
            }
        }

        return false;
    }

    private IPropertyType? GetOrCreateProperty(TObject item,
        Guid key,
        string alias,
        Guid definitionKey,
        string propertyEditorAlias,
        List<string>? compositeProperties,
        out bool IsNew)
    {
        logger.LogDebug("GetOrCreateProperty {key} [{alias}]", key, alias);

        IsNew = false;

        IPropertyType? property = default;

        if (key != Guid.Empty)
        {
            // should we throw - not a valid sync file ?
            // or carry on with best endeavors ? 
            property = item.PropertyTypes.SingleOrDefault(x => x.Key == key);
        }

        // Should we say if we don't have the key? but lets lookup by alias. 
        property ??= item.PropertyTypes.SingleOrDefault(x => x.Alias == alias);

        var editorAlias = propertyEditorAlias;

        IDataType? dataType = default;
        if (definitionKey != Guid.Empty)
        {
            dataType = _dataTypeService.GetAsync(definitionKey).Result;
        }

        if (dataType is null && !string.IsNullOrEmpty(propertyEditorAlias))
        {
            dataType = _dataTypeService.GetAsync(propertyEditorAlias).Result;
        }

        if (dataType is null)
        {
            logger.LogWarning("Cannot find underling DataType {key} {alias} for {property} - Either your datatypes are out of sync or you are missing a package?", definitionKey, propertyEditorAlias, alias);
            return null;
        }

        // we set it here, this means if the file is in conflict (because its changed in the DataType), 
        // we shouldn't reset it later on should we set the editor alias value. 
        editorAlias = dataType.EditorAlias;

        // if it's null then it doesn't exist (so it's new)
        if (property is null)
        {

            if (PropertyExistsOnComposite(item, alias, compositeProperties))
            {
                logger.LogDebug("Cannot create property here {name} as it exist on Composition", item.Name);
                // can't create here, its on a composite
                return null;
            }
            else
            {
                property = new PropertyType(shortStringHelper, dataType, alias);
                IsNew = true;
            }
        }


        // thing that could break if they where blank. 
        // update, only set this if its not already set (because we don't want to break things!)
        // also update it if its not the same as the DataType, (because that has to match)
        if (!property.PropertyEditorAlias.Equals(editorAlias))
        {
            logger.LogDebug("Property Editor Alias mismatch {propertyEditorAlias} != {editorAlias} fixing...", property.PropertyEditorAlias, editorAlias);
            property.PropertyEditorAlias = editorAlias;
        }

        if (property.DataTypeId != dataType.Id)
            property.DataTypeId = dataType.Id;

        return property;

    }


    private static IEnumerable<uSyncChange> MoveProperties(IContentTypeBase item, IDictionary<string, string> moves)
    {
        foreach (var move in moves)
        {
            item.MovePropertyType(move.Key, move.Value);
            yield return uSyncChange.Update($"{move.Key}/Tab/{move.Value}", move.Key, "", move.Value);
        }
    }

    private List<uSyncChange> RemoveProperties(IContentTypeBase item, XElement properties)
    {
        List<string> removals = [];

        var nodes = properties.Elements("GenericProperty")
            .Select(x =>
                new
                {
                    Key = (x.Element(uSyncConstants.Xml.Key).ValueOrDefault(Guid.NewGuid()) == Guid.Empty ? Guid.NewGuid() : x.Element(uSyncConstants.Xml.Key).ValueOrDefault(Guid.NewGuid())),
                    Alias = x.Element(uSyncConstants.Xml.Alias).ValueOrDefault(string.Empty)
                })
            .ToDictionary(k => k.Key, a => a.Alias);


        foreach (var property in item.PropertyTypes)
        {
            if (nodes.ContainsKey(property.Key)) continue;
            if (nodes.Any(x => x.Value.InvariantEquals(property.Alias))) continue;
            removals.Add(property.Alias);
        }

        if (removals.Count != 0)
        {
            var changes = new List<uSyncChange>();

            foreach (var alias in removals)
            {
                // if you remove something with lots of 
                // content this can timeout (still? - need to check on v8)
                logger.LogDebug("Removing {alias}", alias);

                changes.Add(uSyncChange.Delete($"Property/{alias}", alias, ""));

                item.RemovePropertyType(alias);
            }

            return changes;
        }

        return [];
    }

    #endregion

    public override async Task<ChangeType> IsCurrentAsync(XElement node, SyncSerializerOptions options)
    {
        if (node == null) return ChangeType.Update;
        AddStructureSort(node);
        return await base.IsCurrentAsync(node, options);
    }

    private void InsertMissingProperties(XElement node, string propertyName)
    {
        var propertiesNode = node?.Element("GenericProperties");
        if (propertiesNode == null) return;

        foreach (var propertyNode in propertiesNode.Elements("GenericProperty"))
        {
            if (propertyNode.Element(propertyName) == null)
            {
                propertyNode.Add(new XElement(propertyName, string.Empty));
            }
        }
    }

    /// <summary>
    ///  Add missing sort value to XML before compare.
    /// </summary>
    /// <remarks>
    /// Adds the sort order attribute to the XML, if its missing, this reduces the 
    /// number of false positives between old and newer versions of the XML.
    /// and it doesn't cost anywhere as much as the db lookups do.
    /// </remarks>
    private void AddStructureSort(XElement node)
    {
        var structure = node?.Element("Structure");
        if (structure == null || !structure.HasElements) return;

        var sortOrder = 0;
        foreach (var baseNode in structure.Elements(ItemType))
        {
            if (baseNode.Attribute(uSyncConstants.Xml.SortOrder) == null)
            {
                baseNode.Add(new XAttribute(uSyncConstants.Xml.SortOrder, sortOrder++));
            }
        }
    }

    /// <summary>
    ///  does this property alias exist further down the composition tree ? 
    /// </summary>
    protected virtual bool PropertyExistsOnComposite(TObject item, string alias, List<string>? compositeProperties)
    {
        if (compositeProperties == default)
        {
            // passing the compositeProperties list around means we only
            // make this call once per item (and only if we are looking for new properties)
            // should improve the speed of first time syncs.

            // properties that are on the compositions this item is using. 
            compositeProperties = item.ContentTypeComposition?.SelectMany(x => x.PropertyTypes.Select(y => y.Alias)).ToList() ?? [];
        }

        return compositeProperties.Any(existing => existing == alias);
    }


    #region Finders

    public override async Task<TObject?> FindItemAsync(Guid key)
        => await _baseService.GetAsync(key);

    public override Task<TObject?> FindItemAsync(string alias)
        => uSyncTaskHelper.FromResultOf(() => {
            return _baseService.Get(alias);
        });

    public override async Task SaveItemAsync(TObject item) { 
        if (item.IsDirty() is false) return;

        if (item.Id <= 0) {
            await _baseService.CreateAsync(item, Constants.Security.SuperUserKey);
        }
        else {
            await _baseService.UpdateAsync(item, Constants.Security.SuperUserKey);
        }

        //if (item.IsDirty()) _baseService.Save(item);
    }

    public override async Task SaveAsync(IEnumerable<TObject> items) { 
        foreach (var item in items)
            await this.SaveItemAsync(item);
    }

    public override async Task DeleteItemAsync(TObject item)
        => await _baseService.DeleteAsync(item.Key, Constants.Security.SuperUserKey);

    public override string ItemAlias(TObject item)
        => item.Alias;
    #endregion

    #region Tab checks

    //
    // When we create or change a tab type, we need to confirm it won't clash with 
    // an existing tab somewhere else in the tab tree for this item. 
    //
    // This isn't ideal as it involves a full load of all content types 
    // so we try to limit this call so we only do it once if needed per 
    // import, 
    //
    // so the load will only be called if we need to check for a clash 
    // and once we have called it once, we won't call it again for this DocType
    // import.
    //
    // we could go full cache, and keep a history all tab aliases across umbraco
    // but it might not give us enough performance gain for how hard it would be 
    // to ensure it doesn't become out of sync.
    // 

    private Dictionary<string, PropertyGroupType>? _allTabs;

    public bool TabClashesWithExisting(TObject item, string alias, PropertyGroupType tabType)
    {
        EnsureAllTabsCacheLoaded(item);
        return _allTabs?.ContainsKey(alias) is true && _allTabs?[alias] != tabType;
    }

    public void EnsureAllTabsCacheLoaded(TObject item)
    {
        if (_allTabs == null)
        {
            var compositions = item.CompositionPropertyGroups
                .DistinctBy(x => x.Alias)
                .ToDictionary(k => k.Alias, v => v.Type);

            var dependents = _baseService.GetAll()
                .Where(x => x.CompositionIds().Contains(item.Id))
                .SelectMany(x => x.PropertyGroups)
                .DistinctBy(x => x.Alias)
                .ToDictionary(k => k.Alias, v => v.Type);

            _allTabs = compositions
                .Union(dependents.Where(x => !compositions.ContainsKey(x.Key)))
                .ToDictionary(k => k.Key, v => v.Value);
        }
    }

    public void ClearAllTabsCache()
    {
        if (_allTabs != null) _allTabs = null;
    }

    #endregion
}
