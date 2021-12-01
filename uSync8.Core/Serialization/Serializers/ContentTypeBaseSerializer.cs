using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Strings;

using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization.Serializers
{
    public abstract class ContentTypeBaseSerializer<TObject> : SyncContainerSerializerBase<TObject>
        where TObject : IContentTypeComposition
    {
        private readonly IDataTypeService dataTypeService;

        private readonly IContentTypeBaseService<TObject> baseService;

        protected ContentTypeBaseSerializer(
            IEntityService entityService, ILogger logger,
            IDataTypeService dataTypeService,
            IContentTypeBaseService<TObject> baseService,
            UmbracoObjectTypes containerType)
            : base(entityService, logger, containerType)
        {
            this.dataTypeService = dataTypeService;
            this.baseService = baseService;
        }

        #region Serialization 

        protected XElement SerializeBase(TObject item)
        {
            return InitializeBaseNode(item, item.Alias, item.Level);
        }

        protected XElement SerializeInfo(TObject item)
        {
            return new XElement("Info",
                            new XElement("Name", item.Name),
                            new XElement("Icon", item.Icon),
                            new XElement("Thumbnail", item.Thumbnail),
                            new XElement("Description", string.IsNullOrWhiteSpace(item.Description) ? "" : item.Description),
                            new XElement("AllowAtRoot", item.AllowedAsRoot.ToString()),
                            new XElement("IsListView", item.IsContainer.ToString()),
                            new XElement("Variations", item.Variations),
                            new XElement("IsElement", item.IsElement));
        }

        protected XElement SerializeTabs(TObject item)
        {
            var tabs = new XElement("Tabs");

            foreach (var tab in item.PropertyGroups.OrderBy(x => x.SortOrder))
            {
                var tabNode = new XElement("Tab",
                            new XElement("Caption", tab.Name),
                            new XElement("SortOrder", tab.SortOrder));

                if (PropertyGroupExtensions.SupportsTabs)
                {
                    tabNode.Add(new XElement("Alias", tab.GetTabPropertyAsString("Alias")));
                    tabNode.Add(new XElement("Type", tab.GetTabPropertyAsString("Type")));
                }

                tabs.Add(tabNode);

            }

            return tabs;
        }

        protected virtual XElement SerializeProperties(TObject item)
        {
            var node = new XElement("GenericProperties");

            foreach (var property in item.PropertyTypes.OrderBy(x => x.Alias))
            {
                var propNode = new XElement("GenericProperty",
                    new XElement("Key", property.Key),
                    new XElement("Name", property.Name),
                    new XElement("Alias", property.Alias));

                var def = dataTypeService.GetDataType(property.DataTypeId);
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

                var tab = item.PropertyGroups.FirstOrDefault(x => x.PropertyTypes.Contains(property));

                propNode.Add(SerializeTab(item, property));

                SerializeExtraProperties(propNode, item, property);

                // hello ugly if statements. - we could 'just' reflect these everytime.
                // but the if check is quicker (reflection is still quick) so it reduces sync time)

                // added in v8.6
                if (UmbracoVersion.LocalVersion.Major > 8 || UmbracoVersion.LocalVersion.Minor >= 6)
                {
                    SerializeNewProperty<string>(propNode, property, "MandatoryMessage");
                    SerializeNewProperty<string>(propNode, property, "ValidationRegExpMessage");
                }

                // added in 8.10
                if (UmbracoVersion.LocalVersion.Major > 8 || UmbracoVersion.LocalVersion.Minor >= 10)
                {
                    SerializeNewProperty<bool>(propNode, property, "LabelOnTop");
                }

                node.Add(propNode);
            }

            return node;
        }

        protected XElement SerializeTab(TObject item, PropertyType property)
        {
            var tab = item.PropertyGroups.FirstOrDefault(x => x.PropertyTypes.Contains(property));
            var tabNode = new XElement("Tab", tab != null ? tab.Name : "");

            if (tab != null)
            {
                var alias = tab.GetTabPropertyAsString("Alias");
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    tabNode.Add(new XAttribute("Alias", alias));
                }
            }

            return tabNode;
        }

        /// <summary>
        ///  Serialize properties that have been introduced in later versions of umbraco.
        /// </summary>
        /// <remarks>
        ///  by doing this like this it makes us keep our backwards compatability, while also supporting 
        ///  newer properties. 
        /// </remarks>
        protected void SerializeNewProperty<TValue>(XElement node, PropertyType property, string propertyName)
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


        protected virtual void SerializeExtraProperties(XElement node, TObject item, PropertyType property)
        {
            // when something has extra properties that the others don't (memberTypes at the moment)
        }

        protected XElement SerializeStructure(TObject item)
        {
            var node = new XElement("Structure");
            List<KeyValuePair<string, string>> items = new List<KeyValuePair<string, string>>();

            foreach (var allowedType in item.AllowedContentTypes.OrderBy(x => x.SortOrder))
            {
                var allowedItem = FindItem(allowedType.Id.Value);
                if (allowedItem != null)
                {
                    node.Add(new XElement(ItemType,
                        new XAttribute("Key", allowedItem.Key),
                        new XAttribute("SortOrder", allowedType.SortOrder), allowedItem.Alias));
                }
            }
            return node;
        }

        protected XElement SerializeCompostions(ContentTypeCompositionBase item)
        {
            var compNode = new XElement("Compositions");
            var compositions = item.ContentTypeComposition;
            foreach (var composition in compositions.OrderBy(x => x.Alias))
            {
                compNode.Add(new XElement("Composition", composition.Alias,
                    new XAttribute("Key", composition.Key)));
            }

            return compNode;
        }

        #endregion

        #region Deserialization


        protected IEnumerable<uSyncChange> DeserializeBase(TObject item, XElement node)
        {
            logger.Debug(serializerType, "Deserializing Base");

            if (node == null) return Enumerable.Empty<uSyncChange>();

            var info = node.Element("Info");
            if (info == null) return Enumerable.Empty<uSyncChange>();

            List<uSyncChange> changes = new List<uSyncChange>();

            var key = node.GetKey();
            if (item.Key != key)
            {
                changes.AddUpdate("Key", item.Key, key, "");
                item.Key = key;
            }


            var alias = node.GetAlias();
            if (item.Alias != alias)
            {
                changes.AddUpdate("Alias", item.Alias, alias, "");
                item.Alias = alias;
            }

            var name = info.Element("Name").ValueOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(name) && item.Name != name)
            {
                changes.AddUpdate("Name", item.Name, name, "");
                item.Name = name;
            }

            var icon = info.Element("Icon").ValueOrDefault(string.Empty);
            if (item.Icon != icon)
            {
                changes.AddUpdate("Icon", item.Icon, icon, "");
                item.Icon = icon;
            }

            var thumbnail = info.Element("Thumbnail").ValueOrDefault(string.Empty);
            if (item.Thumbnail != thumbnail)
            {
                changes.AddUpdate("Icon", item.Thumbnail, thumbnail, "");
                item.Thumbnail = thumbnail;
            }

            var description = info.Element("Description").ValueOrDefault(null);
            if (item.Description != description)
            {
                changes.AddUpdate("Description", item.Description, description, "");
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

            var isContainer = info.Element("IsListView").ValueOrDefault(false);
            if (item.IsContainer != isContainer)
            {
                changes.AddUpdate("IsListView", item.IsContainer, isContainer, "");
                item.IsContainer = isContainer;
            }

            if (!SetMasterFromElement(item, info.Element("Parent")))
            {
                SetFolderFromElement(item, info.Element("Folder"));
            }

            return changes;
        }

        protected IEnumerable<uSyncChange> DeserializeStructure(TObject item, XElement node)
        {
            logger.Debug(serializerType, "Deserializing Structure");

            var structure = node.Element("Structure");
            if (structure == null) return Enumerable.Empty<uSyncChange>();

            var changes = new List<uSyncChange>();

            List<ContentTypeSort> allowed = new List<ContentTypeSort>();
            int sortOrder = 0;

            foreach (var baseNode in structure.Elements(ItemType))
            {
                logger.Debug(serializerType, "baseNode {0}", baseNode.ToString());
                var alias = baseNode.Value;
                var key = baseNode.Attribute("Key").ValueOrDefault(Guid.Empty);

                logger.Debug(serializerType, "Structure: {0}", key);

                var itemSortOrder = baseNode.Attribute("SortOrder").ValueOrDefault(sortOrder);
                logger.Debug(serializerType, "Sort Order: {0}", itemSortOrder);

                IContentTypeBase baseItem = default(IContentTypeBase);

                if (key != Guid.Empty)
                {
                    logger.Debug(serializerType, "Structure By Key {0}", key);
                    // lookup by key (our prefered way)
                    baseItem = FindItem(key);
                }

                if (baseItem == null)
                {
                    logger.Debug(serializerType, "Structure By Alias: {0}", alias);
                    // lookup by alias (less nice)
                    baseItem = FindItem(alias);
                }

                if (baseItem != null)
                {
                    logger.Debug(serializerType, "Structure Found {0}", baseItem.Alias);
                    allowed.Add(new ContentTypeSort(baseItem.Id, itemSortOrder));
                    sortOrder = itemSortOrder + 1;
                }
            }

            logger.Debug(serializerType, "Structure: {0} items", allowed.Count);

            // compare the two lists (the equality compare fails because the id value is lazy)
            var currentHash =
                string.Join(":", item.AllowedContentTypes.Select(x => $"{x.Id.Value}-{x.SortOrder}"));

            var newHash =
                string.Join(":", allowed.Select(x => $"{x.Id.Value}-{x.SortOrder}"));

            if (!currentHash.Equals(newHash))
            {
                changes.AddUpdate("Allowed", 
                    string.Join(",", item.AllowedContentTypes.Select(x => x.Alias) ?? Enumerable.Empty<string>()),
                    string.Join(",", allowed.Select(x => x.Alias) ?? Enumerable.Empty<string>()), "/Structure");

                logger.Debug(serializerType, "Updating allowed content types");
                item.AllowedContentTypes = allowed;
            }

            return changes;
        }

        protected IEnumerable<uSyncChange> DeserializeProperties(TObject item, XElement node, SyncSerializerOptions options)
        {
            logger.Debug(serializerType, "Deserializing Properties");

            var propertiesNode = node?.Element("GenericProperties");
            if (propertiesNode == null) return Enumerable.Empty<uSyncChange>();

            /// there are something we can't do in the loop, 
            /// so we store them and do them once we've put 
            /// things in. 
            List<string> propertiesToRemove = new List<string>();
            Dictionary<string, string> propertiesToMove = new Dictionary<string, string>();

            List<uSyncChange> changes = new List<uSyncChange>();

            foreach (var propertyNode in propertiesNode.Elements("GenericProperty"))
            {
                var alias = propertyNode.Element("Alias").ValueOrDefault(string.Empty);
                if (string.IsNullOrEmpty(alias)) continue;

                var key = propertyNode.Element("Key").ValueOrDefault(alias.GetHashCode().ToGuid());
                var definitionKey = propertyNode.Element("Definition").ValueOrDefault(Guid.Empty);
                var propertyEditorAlias = propertyNode.Element("Type").ValueOrDefault(string.Empty);

                logger.Debug(serializerType, " > Property: {0} {1} {2} {3}", alias, key, definitionKey, propertyEditorAlias);

                bool IsNew = false;

                var property = GetOrCreateProperty(item, key, alias, definitionKey, propertyEditorAlias, out IsNew);
                if (property == null) continue;

                if (key != Guid.Empty && property.Key != key)
                {
                    changes.AddUpdate("Key", property.Key, key, $"{alias}/Key");
                    property.Key = key;
                }

                if (property.Alias != alias)
                {
                    changes.AddUpdate("Alias", property.Alias, alias, $"{alias}/Alias");
                    property.Alias = alias;
                }

                var name = propertyNode.Element("Name").ValueOrDefault(alias);
                if (property.Name != name)
                {
                    changes.AddUpdate("Name", property.Name, name, $"{alias}/Name");
                    property.Name = name;
                }

                var description = propertyNode.Element("Description").ValueOrDefault(string.Empty);
                if (property.Description != description)
                {
                    changes.AddUpdate("Description", property.Description, description, $"{alias}/Description");
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
                    changes.AddUpdate("Validation", property.ValidationRegExp, regEx, $"{alias}/RegEx");
                    property.ValidationRegExp = propertyNode.Element("Validation").ValueOrDefault(string.Empty);
                }

                var sortOrder = propertyNode.Element("SortOrder").ValueOrDefault(0);
                if (property.SortOrder != sortOrder)
                {
                    changes.AddUpdate("SortOrder", property.SortOrder, sortOrder, $"{alias}/SortOrder");
                    property.SortOrder = sortOrder;
                }

                // added in v8.6
                // reflection is fast but a a quick check of version is faster !
                if (UmbracoVersion.LocalVersion.Major > 8 || UmbracoVersion.LocalVersion.Minor >= 6)
                {
                    changes.AddNotNull(DeserializeNewProperty<string>(property, propertyNode, "MandatoryMessage"));
                    changes.AddNotNull(DeserializeNewProperty<string>(property, propertyNode, "ValidationRegExpMessage"));
                }

                if (UmbracoVersion.LocalVersion.Major > 8 || UmbracoVersion.LocalVersion.Minor >= 10)
                {
                    changes.AddNotNull(DeserializeNewProperty<bool>(property, propertyNode, "LabelOnTop"));
                }

                changes.AddRange(DeserializeExtraProperties(item, property, propertyNode));

                var tabAliasOrName = item.PropertyGroups.GetTabAliasOrName(propertyNode.Element("Tab"));
                var tabName = propertyNode.Element("Tab").ValueOrDefault(tabAliasOrName);

                if (IsNew)
                {
                    changes.AddNew(alias, name, alias);
                    logger.Debug(serializerType, "Property Is new adding to tab. {property} [{tabNameOrAlias}] [{name}]", property.Name, tabAliasOrName, tabName);
                    var tabGroup = item.PropertyGroups.FindTab(tabAliasOrName, tabName);
                    if (tabGroup != null)
                    {
                        item.SafeAddPropertyType(property, tabGroup.GetTabAliasOrName(), tabGroup.Name);
                    }
                    else
                    {
                        logger.Warn(serializerType, "Cannot find tab {tabAliasOrName} to add {property} to", tabAliasOrName, property.Alias);
                    }
                }
                else
                {
                    logger.Debug(serializerType, "Property exists, checking tab location {tabAliasOrName} {name}", tabAliasOrName, tabName);
                    // we need to see if this one has moved. 
                    if (!string.IsNullOrWhiteSpace(tabAliasOrName))
                    {
                        // index of key - will fail back from alias to name,
                        // so it simplifies this call for v8
                        var tabGroup = item.PropertyGroups.FindTab(tabAliasOrName, tabName);
                        if (tabGroup != null)
                        {
                            // this tab doesn't currently contain this property. 
                            if (!tabGroup.PropertyTypes.Contains(property.Alias))
                            {
                                // add to our move list.
                                // we have to get the alias again, it might be a temp one.
                                propertiesToMove[property.Alias] = tabGroup.GetTabAliasOrName();
                            }
                        }
                        else
                        {
                            logger.Warn(serializerType, "Cannot find tab {tabAliasOrName} to add {property} to", tabAliasOrName, property.Alias);
                        }
                    }
                }
            }

            // move things between tabs. 
            changes.AddRange(MoveProperties(item, propertiesToMove));

            if (options.DeleteItems())
            {
                // remove what needs to be removed
                changes.AddRange(RemoveProperties(item, propertiesNode));
            }
            else
            {
                logger.Debug(serializerType, "Property Removal disabled by config");
            }

            return changes;

        }


     
        /// <summary>
        ///  Deserialize properties added in later versions of Umbraco.
        /// </summary>
        /// <remarks>
        ///  using reflection to find properties that might have been added in later versions of umbraco.
        ///  doing it this way means we can maintain backwards compatability.
        /// </remarks>
        protected uSyncChange DeserializeNewProperty<TValue>(PropertyType property, XElement node, string propertyName)
        {
            var propertyInfo = property?.GetType()?.GetProperty(propertyName);
            if (propertyInfo != null)
            {
                var value = node.Element(propertyName).ValueOrDefault(string.Empty);
                var attempt = value.TryConvertTo<TValue>();
                if (attempt.Success)
                {
                    var current = GetPropertyAs<TValue>(propertyInfo, property);

                    if (current == null || !current.Equals(attempt.Result))
                    {
                        propertyInfo.SetValue(property, attempt.Result);

                        return uSyncChange.Update($"property/{propertyName}",
                            propertyName,
                            current?.ToString() ?? "(Blank)",
                            attempt.Result.ToString());
                    }
                }
            }

            return null;
        }

        private TValue GetPropertyAs<TValue>(PropertyInfo info, PropertyType property)
        {
            if (info == null) return default;

            var value = info.GetValue(property);
            if (value == null) return default;

            var result = value.TryConvertTo<TValue>();
            if (result.Success)
                return result.Result;

            return default;

        }


        virtual protected IEnumerable<uSyncChange> DeserializeExtraProperties(TObject item, PropertyType property, XElement node)
        {
            // nothing.
            return Enumerable.Empty<uSyncChange>();
        }

        protected IEnumerable<uSyncChange> DeserializeTabs(TObject item, XElement node)
        {
            logger.Debug(serializerType, "Deserializing Tabs");

            var tabNode = node.Element("Tabs");
            if (tabNode == null) return Enumerable.Empty<uSyncChange>();

            var defaultSort = 0;
            var defaultTabType = tabNode.GetDefaultTabType();

            var changes = new List<uSyncChange>();

            foreach (var tab in tabNode.Elements("Tab"))
            {
                try
                {
                    var tabName = tab.Element("Caption").ValueOrDefault(string.Empty);
                    var sortOrder = tab.Element("SortOrder").ValueOrDefault(defaultSort);
                    var aliasOrName = tab.Element("Alias").ValueOrDefault(tabName);
                    var type = tab.Element("Type").ValueOrDefault(defaultTabType);

                    logger.Debug(serializerType, "> Tab Name: {0} Alias: {1} Sort: {2} Type : [{3}]", tabName, aliasOrName, sortOrder, type);

                    var existing = item.PropertyGroups.FindTab(aliasOrName, tabName);
                    if (existing != null)
                    {
                        if (PropertyGroupExtensions.SupportsTabs)
                        {
                            // because we search case insensitive for alias name it might not 
                            // match exactly when we find it.
                            if (existing.GetTabAliasOrName() != aliasOrName)
                                existing.SetGroupAlias(aliasOrName);
                        }

                        if (existing.SortOrder != sortOrder)
                        {
                            changes.AddUpdate("SortOrder", existing.SortOrder, sortOrder, $"Tabs/{tabName}/SortOrder");
                            existing.SortOrder = sortOrder;
                        }

                        if (existing.Name != tabName)
                        {
                            changes.AddUpdate("Name", existing.Name, tabName, $"Tabs/{tabName}/Name");
                            existing.Name = tabName;
                        }

                        var existingType = existing.GetTabPropertyAsString("Type");
                        if (!string.IsNullOrWhiteSpace(existingType) && existingType != type)
                        {
                            logger.Debug(serializerType, "{alias} tab type changed {from} > {to}", aliasOrName, type, existing);
                            if (TabClashesWithExisting(item, aliasOrName, type))
                            {
                                // v8.17 - you can't just swap from group to tab
                                //   if the tab is used in other places this can cause a clash
                                //   so we prefix the alias, and in the second step we clean it
                                existing.SetGroupAlias(PropertyGroupExtensions.GetTempTabAlias(aliasOrName));
                            }

                            changes.AddUpdate("Type", existingType, type, $"Tabs/{tabName}/Type");
                            existing.SetGroupType(type);
                        }
                    }
                    else
                    {
                        var safeAliasName = aliasOrName;

                        // only do the safe alias thing if we don't support tabs. 
                        if (!PropertyGroupExtensions.SupportsTabs)
                            safeAliasName = aliasOrName.ToSafeAlias(true);

                        if (TabClashesWithExisting(item, safeAliasName, type))
                            safeAliasName = PropertyGroupExtensions.GetTempTabAlias(safeAliasName);

                        item.SafeAddPropertyGroup(safeAliasName, tabName);

                        changes.AddNew(tabName, tabName, $"Tabs/{tabName}");
                        var newTab = item.PropertyGroups.FindTab(safeAliasName, tabName);
                        if (newTab != null)
                        {
                            newTab.SortOrder = sortOrder;

                            // set the tab type through relection
                            newTab.SetGroupType(type);
                        }
                    }

                    defaultSort = sortOrder + 1;
                }
                catch(Exception ex)
                {
                    logger.Warn(serializerType, ex, "Error attempting to deserialize tabs for {item}", item.Alias);
                }
            }

            ClearAllTabsCache();

            return changes;
        }

        /// <summary>
        ///  remove any prefixes we may have added to a tab alias
        /// </summary>
        /// <param name="item"></param>
        protected void CleanTabAliases(TObject item)
        {
            if (PropertyGroupExtensions.SupportsTabs)
            {
                foreach (var tab in item.PropertyGroups)
                {
                    var alias = tab.GetTabAliasOrName();
                    if (PropertyGroupExtensions.IsTempTabAlias(alias))
                    {
                        tab.SetGroupAlias(PropertyGroupExtensions.StripTempTabAlias(alias));
                    }
                }
            }
        }

        protected IEnumerable<uSyncChange> CleanTabs(TObject item, XElement node, SyncSerializerOptions options)
        {
            if (options.DeleteItems())
            {
                logger.Debug(serializerType, "Cleaning Tabs Base");

                var tabNode = node?.Element("Tabs");
                if (tabNode == null) return Enumerable.Empty<uSyncChange>();

                var tabAliases = tabNode.Elements("Tab")
                    .Where(x => x.Element("Alias") != null)
                    .Select(x => x.Element("Alias").ValueOrDefault(string.Empty));

                var tabNames = tabNode.Elements("Tab")
                    .Where(x => x.Element("Caption") != null)
                    .Select(x => x.Element("Caption").ValueOrDefault(string.Empty));

                var namesOrAlias = tabAliases.Concat(tabNames);

                var newTabAliasOrNames = tabNode.Elements("Tab")
                    .Where(x => x.Element("Alias") != null || x.Element("Caption") != null)
                    .Select(x => x.Element("Alias").ValueOrDefault(x.Element("Caption").ValueOrDefault(string.Empty)))
                    .ToList();

                List<int> removals = new List<int>();
                foreach (var tab in item.PropertyGroups)
                {
                    if (PropertyGroupExtensions.SupportsTabs && tabAliases.Any())
                    {
                        // look by alias 
                        var tabAlias = tab.GetTabPropertyAsString("Alias");
                        if (!string.IsNullOrWhiteSpace(tabAlias) && !tabAliases.InvariantContains(tabAlias))
                        {
                            logger.Debug(serializerType, "Cannot find tab by tab Alias : {tabAlias}");
                            removals.Add(tab.Id);
                        }
                    }
                    else
                    {
                        // look by name. 
                        if (!tabNames.InvariantContains(tab.Name))
                        {
                            logger.Debug(serializerType, "Cannot find tab by name : {tabAlias}");
                            removals.Add(tab.Id);
                        }
                    }
                }

                if (removals.Count > 0)
                {
                    var changes = new List<uSyncChange>();


                    foreach (var tabId in removals)
                    {
                        var tab = item.PropertyGroups.FirstOrDefault(x => x.Id == tabId);

                        if (tab != null)
                        {
                            logger.Debug(serializerType, "Removing tab : {0}", tabId);
                            changes.Add(uSyncChange.Delete($"Tabs/{tabId}", tabId.ToString(), tabId.ToString()));

                            if (tab.PropertyTypes.Count > 0)
                            {
                                logger.Warn(serializerType, "Tab/group {name} still contains properties, we will not remove it. [{properties}]", tab.Name,
                                    string.Join(",", tab.PropertyTypes.Select(x => x.Name)));
                            }
                            else
                            {
                                // remove in core has some fallbakc if we pass in alias, it will remove by that
                                // if we pass in name it does it by name. 
                                item.PropertyGroups.Remove(tab);
                            }
                        }

                    }

                    return changes;
                }
            }

            return Enumerable.Empty<uSyncChange>();
        }

        protected void CleanFolder(TObject item, XElement node)
        {
            var folderNode = node.Element("Info").Element("Folder");
            if (folderNode != null)
            {
                logger.Debug(serializerType, "Cleaing Folder");

                var key = folderNode.Attribute("Key").ValueOrDefault(Guid.Empty);
                if (key != Guid.Empty)
                {
                    logger.Debug(serializerType, "Folder Key {0}", key.ToString());
                    var folder = FindContainer(key);
                    if (folder == null)
                    {
                        logger.Debug(serializerType, "Folder Key doesn't not match");
                        FindFolder(key, folderNode.Value);
                    }
                }
            }
        }

        protected IEnumerable<uSyncChange> DeserializeCompositions(TObject item, XElement node)
        {
            logger.Debug(serializerType, "Deserializing Compositions");

            var comps = node?.Element("Info")?.Element("Compositions");
            if (comps == null) return Enumerable.Empty<uSyncChange>();

            List<IContentTypeComposition> compositions = new List<IContentTypeComposition>();

            foreach (var compositionNode in comps.Elements("Composition"))
            {
                var alias = compositionNode.Value;
                var key = compositionNode.Attribute("Key").ValueOrDefault(Guid.Empty);

                logger.Debug(serializerType, " > Comp {0} {1}", alias, key);

                var type = FindItem(key, alias);
                if (type != null)
                    compositions.Add(type);
            }

            // compare hashes, because enumerable compare fails with the lazy properties in the collections
            var currentHash = string.Join(":", item.ContentTypeComposition.Select(x => $"{x.Alias}-{x.SortOrder}").OrderBy(x => x));
            var newHash = string.Join(":", compositions.Select(x => $"{x.Alias}-{x.SortOrder}").OrderBy(x => x));

            if (currentHash != newHash)
            {
                var change = uSyncChange.Update("Info", "Compositions",
                    string.Join(",", item.ContentTypeComposition.Select(x => x.Alias)),
                    string.Join(",", compositions.Select(x => x.Alias)));

                item.ContentTypeComposition = compositions;

                return change.AsEnumerableOfOne();
            }

            return Enumerable.Empty<uSyncChange>();
        }


        private void SetFolderFromElement(IContentTypeBase item, XElement folderNode)
        {
            var folder = folderNode.ValueOrDefault(string.Empty);
            if (string.IsNullOrWhiteSpace(folder)) return;

            var container = FindFolder(folderNode.GetKey(), folder);
            if (container != null && container.Id != item.ParentId)
            {
                item.SetParent(container);
            }
        }


        private bool SetMasterFromElement(IContentTypeBase item, XElement masterNode)
        {
            logger.Debug(serializerType, "SetMasterFromElement");

            if (masterNode == null) return false;

            var key = masterNode.Attribute("Key").ValueOrDefault(Guid.Empty);
            if (key != Guid.Empty)
            {
                var entity = entityService.Get(key);
                if (entity != null && entity.Id != item.ParentId)
                {
                    item.SetParent(entity);
                    return true;
                }
            }

            return false;
        }

        private PropertyType GetOrCreateProperty(TObject item,
            Guid key,
            string alias,
            Guid definitionKey,
            string propertyEditorAlias,
            out bool IsNew)
        {
            logger.Debug(serializerType, "GetOrCreateProperty {0} [{1}]", key, alias);

            IsNew = false;

            var property = default(PropertyType);

            if (key != Guid.Empty)
            {
                // should we throw - not a valid sync file ?
                // or carry on with best endevours ? 
                property = item.PropertyTypes.SingleOrDefault(x => x.Key == key);
            }

            if (property == null)
            {
                // we should really say if we don't have the key! 
                // but lets lookup by alias. 
                property = item.PropertyTypes.SingleOrDefault(x => x.Alias == alias);
            }

            var editorAlias = propertyEditorAlias;

            IDataType dataType = default(IDataType);
            if (definitionKey != Guid.Empty)
            {
                dataType = dataTypeService.GetDataType(definitionKey);
            }

            if (dataType == null && !string.IsNullOrEmpty(propertyEditorAlias))
            {
                dataType = dataTypeService.GetDataType(propertyEditorAlias);
            }

            if (dataType == null) return null;

            // we set it here, this means if the file is in conflict (because its changed in the datatype), 
            // we shouldn't reset it later on should we set the editor alias value. 
            editorAlias = dataType.EditorAlias;

            // if it's null then it doesn't exist (so it's new)
            if (property == null)
            {
                if (PropertyExistsOnComposite(item, alias))
                {
                    logger.Debug(serializerType, "Cannot create property here {0} as it exist on Composition", item.Name);
                    // can't create here, its on a composite
                    return null;
                }
                else
                {
                    property = new PropertyType(dataType, alias);
                    IsNew = true;
                }
            }


            // thing that could break if they where blank. 
            // update, only set this if its not already set (because we don't want to break things!)
            // also update it if its not the same as the datatype, (because that has to match)
            if (!property.PropertyEditorAlias.Equals(editorAlias))
            {
                logger.Debug(serializerType, "Property Editor Alias mismatch {0} != {1} fixing...", property.PropertyEditorAlias, editorAlias);
                property.PropertyEditorAlias = editorAlias;
            }

            if (property.DataTypeId != dataType.Id)
                property.DataTypeId = dataType.Id;

            return property;

        }


        private IEnumerable<uSyncChange> MoveProperties(IContentTypeBase item, IDictionary<string, string> moves)
        {
            foreach (var move in moves)
            {
                logger.Debug(serializerType, "Moving property {property} to tab/group {tab}", move.Key, move.Value);
                item.MovePropertyType(move.Key, move.Value);

                yield return uSyncChange.Update($"{move.Key}/Tab/{move.Value}", move.Key, "", move.Value);
            }
        }

        private IEnumerable<uSyncChange> RemoveProperties(IContentTypeBase item, XElement properties)
        {
            List<string> removals = new List<string>();

            var nodes = properties.Elements("GenericProperty")
                .Select(x =>
                    new
                    {
                        Key = (x.Element("Key").ValueOrDefault(Guid.NewGuid()) == Guid.Empty ? Guid.NewGuid() : x.Element("Key").ValueOrDefault(Guid.NewGuid())),
                        Alias = x.Element("Alias").ValueOrDefault(string.Empty)
                    })
                .ToDictionary(k => k.Key, a => a.Alias);


            foreach (var property in item.PropertyTypes)
            {
                if (nodes.ContainsKey(property.Key)) continue;
                if (nodes.Any(x => x.Value.InvariantEquals(property.Alias))) continue;
                removals.Add(property.Alias);
            }

            if (removals.Any())
            {
                var changes = new List<uSyncChange>();

                foreach (var alias in removals)
                {
                    // if you remove something with lots of 
                    // content this can timeout (still? - need to check on v8)
                    logger.Debug(serializerType, "Removing {0}", alias);

                    changes.Add(uSyncChange.Delete($"Property/{alias}", alias, ""));

                    item.RemovePropertyType(alias);
                }

                return changes;
            }

            return Enumerable.Empty<uSyncChange>();
        }

        #endregion


        public override ChangeType IsCurrent(XElement node, SyncSerializerOptions options)
        {
            if (node == null) return ChangeType.Update;

            // checkfor and add missing 8.6 properties, means we get less false positives.
            if (UmbracoVersion.LocalVersion.Major > 8 || UmbracoVersion.LocalVersion.Minor >= 6)
            {
                InsertMissingProperties(node, "MandatoryMessage");
                InsertMissingProperties(node, "ValidationRegExpMessage");
            }

            AddStructureSort(node);

            return base.IsCurrent(node, options);
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
        ///  Add missing sort value to xml before compare.
        /// </summary>
        /// <remarks>
        /// Adds the sort order attribute to the xml, if its missing, this reduces the 
        /// number of false positives between old and newer versions of the xml.
        /// and it doesn't cost anywhere as much as the db lookups do.
        /// </remarks>
        private void AddStructureSort(XElement node)
        {
            var structure = node?.Element("Structure");
            if (structure == null || !structure.HasElements) return;

            var sortOrder = 0;
            foreach (var baseNode in structure.Elements(ItemType))
            {
                if (baseNode.Attribute("SortOrder") == null)
                {
                    baseNode.Add(new XAttribute("SortOrder", sortOrder++));
                }
            }
        }

        /// <summary>
        ///  does this property alias exist further down the composition tree ? 
        /// </summary>
        protected virtual bool PropertyExistsOnComposite(TObject item, string alias)
        {
            var allTypes = baseService.GetAll().ToList();

            var allProperties = allTypes
                    .Where(x => x.ContentTypeComposition.Any(y => y.Id == item.Id))
                    .Select(x => x.PropertyTypes)
                    .ToList();

            return allProperties.Any(x => x.Any(y => y.Alias == alias));
        }


        #region Finders

        protected virtual TObject FindItem(int id)
            => baseService.Get(id);

        override protected TObject FindItem(Guid key)
            => baseService.Get(key);

        override protected TObject FindItem(string alias)
            => baseService.Get(alias);


        override protected EntityContainer FindContainer(Guid key)
            => baseService.GetContainer(key);

        override protected IEnumerable<EntityContainer> FindContainers(string folder, int level)
            => baseService.GetContainers(folder, level);

        override protected Attempt<OperationResult<OperationResultType, EntityContainer>> FindContainers(int parentId, string name)
            => baseService.CreateContainer(parentId, name);

        protected override void SaveItem(TObject item)
        {
            if (item.IsDirty()) baseService.Save(item);
        }


        public override void Save(IEnumerable<TObject> items)
            => baseService.Save(items);

        protected override void SaveContainer(EntityContainer container)
        {
            logger.Debug(serializerType, "Saving Container: {0}", container.Key);
            baseService.SaveContainer(container);
        }

        protected override void DeleteItem(TObject item)
            => baseService.Delete(item);

        protected override string ItemAlias(TObject item)
            => item.Alias;

        protected override IEnumerable<EntityContainer> GetContainers(TObject item)
            => baseService.GetContainers(item);

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
        // and once we have called it once, we won't call it again for this doctype
        // import.
        //
        // we could go full cache, and keep a history all all tab aliases across umbraco
        // but it might not give us enough performance gain for how hard it would be 
        // to ensure it doesn't become out of sync.
        // 

        private Dictionary<string, string> _allTabs;

        public bool TabClashesWithExisting(TObject item, string alias, string tabType)
        {
            if (PropertyGroupExtensions.SupportsTabs)
            {
                EnsureAllTabsCacheLoaded(item);
                return _allTabs.ContainsKey(alias) && _allTabs[alias] != tabType;
            }
            return false;
        }

        public void EnsureAllTabsCacheLoaded(TObject item)
        {
            if (_allTabs == null)
            {
                var compositions = item.CompositionPropertyGroups
                    .DistinctBy(x => x.GetTabAliasOrName())
                    .ToDictionary(k => k.GetTabAliasOrName(), v => v.GetTabPropertyAsString("Type"));

                var dependents = baseService.GetAll()
                    .Where(x => x.CompositionIds().Contains(item.Id))
                    .SelectMany(x => x.PropertyGroups)
                    .DistinctBy(x => x.GetTabAliasOrName())
                    .ToDictionary(k => k.GetTabAliasOrName(), v => v.GetTabPropertyAsString("Type"));

                _allTabs = compositions
                    .Union(dependents.Where(x => !compositions.ContainsKey(x.Key)))
                    .ToDictionary(k => k.Key, v=>v.Value);
            }
        }

        public void ClearAllTabsCache()
        {
            if (_allTabs != null) _allTabs = null;
        }

        #endregion
    }
}
