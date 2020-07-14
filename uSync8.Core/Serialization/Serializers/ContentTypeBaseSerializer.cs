using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
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
                tabs.Add(new XElement("Tab",
                            new XElement("Caption", tab.Name),
                            new XElement("SortOrder", tab.SortOrder)));
            }

            return tabs;
        }

        protected XElement SerializeProperties(TObject item)
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
                propNode.Add(new XElement("Tab", tab != null ? tab.Name : ""));

                SerializeExtraProperties(propNode, item, property);

                node.Add(propNode);
            }

            return node;
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
    

        protected void DeserializeBase(TObject item, XElement node)
        {
            logger.Debug(serializerType, "Deserializing Base");

            if (node == null) return;

            var key = node.GetKey();
            if (item.Key != key)
                item.Key = key;

            var info = node.Element("Info");
            if (info == null) return;

            var alias = node.GetAlias();
            if (item.Alias != alias)
                item.Alias = alias;

            var name = info.Element("Name").ValueOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(name) && item.Name != name)
                item.Name = name;

            var icon = info.Element("Icon").ValueOrDefault(string.Empty);
            if (item.Icon != icon)
                item.Icon = icon;

            var thumbnail = info.Element("Thumbnail").ValueOrDefault(string.Empty);
            if (item.Thumbnail != thumbnail)
                item.Thumbnail = thumbnail;

            var description = info.Element("Description").ValueOrDefault(null);
            if (item.Description != description)
                item.Description = description;

            var allowedAsRoot = info.Element("AllowAtRoot").ValueOrDefault(false);
            if (item.AllowedAsRoot != allowedAsRoot)
                item.AllowedAsRoot = allowedAsRoot;

            var variations = info.Element("Variations").ValueOrDefault(ContentVariation.Nothing);
            if (item.Variations != variations)
                item.Variations = variations;

            var isElement = info.Element("IsElement").ValueOrDefault(false);
            if (item.IsElement != isElement)
                item.IsElement = isElement;

            var isContainer = info.Element("IsListView").ValueOrDefault(false);
            if (item.IsContainer != isContainer)
                item.IsContainer = isContainer;

            if (!SetMasterFromElement(item, info.Element("Parent")))
            {
                SetFolderFromElement(item, info.Element("Folder"));
            }

        }

        protected void DeserializeStructure(TObject item, XElement node)
        {
            logger.Debug(serializerType, "Deserializing Structure");


            var structure = node.Element("Structure");
            if (structure == null) return;

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
                logger.Debug(serializerType, "Updating allowed content types");
                item.AllowedContentTypes = allowed;
            }
        }

        protected void DeserializeProperties(TObject item, XElement node)
        {
            logger.Debug(serializerType, "Deserializing Properties");


            if (node == null) return;

            var propertiesNode = node.Element("GenericProperties");
            if (propertiesNode == null) return;

            /// there are something we can't do in the loop, 
            /// so we store them and do them once we've put 
            /// things in. 
            List<string> propertiesToRemove = new List<string>();
            Dictionary<string, string> propertiesToMove = new Dictionary<string, string>();

            foreach (var propertyNode in propertiesNode.Elements("GenericProperty"))
            {
                var alias = propertyNode.Element("Alias").ValueOrDefault(string.Empty);
                if (string.IsNullOrEmpty(alias)) continue;


                var key = propertyNode.Element("Key").ValueOrDefault(Guid.Empty);
                var definitionKey = propertyNode.Element("Definition").ValueOrDefault(Guid.Empty);
                var propertyEditorAlias = propertyNode.Element("Type").ValueOrDefault(string.Empty);

                logger.Debug(serializerType, " > Property: {0} {1} {2} {3}", alias, key, definitionKey, propertyEditorAlias);

                bool IsNew = false;

                var property = GetOrCreateProperty(item, key, alias, definitionKey, propertyEditorAlias, out IsNew);
                if (property == null) continue;

                if (key != Guid.Empty && property.Key != key)
                    property.Key = key;

                // do we trust the core ? - because in theory 
                // we can set the value, and it will only
                // be updated if marked dirty and that will
                // only happen if the value is different ?

                property.Alias = alias;
                property.Name = propertyNode.Element("Name").ValueOrDefault(alias);
                property.Description = propertyNode.Element("Description").ValueOrDefault(string.Empty);
                property.Mandatory = propertyNode.Element("Mandatory").ValueOrDefault(false);
                property.ValidationRegExp = propertyNode.Element("Validation").ValueOrDefault(string.Empty);
                property.SortOrder = propertyNode.Element("SortOrder").ValueOrDefault(0);

                DeserializeExtraProperties(item, property, propertyNode);

                var tab = propertyNode.Element("Tab").ValueOrDefault(string.Empty);

                if (IsNew)
                {
                    logger.Debug(serializerType, "Property Is new adding to tab.");
                    if (string.IsNullOrWhiteSpace(tab))
                    {
                        item.AddPropertyType(property);
                    }
                    else
                    {
                        item.AddPropertyType(property, tab);
                    }
                }
                else
                {
                    logger.Debug(serializerType, "Property exists, checking tab location");
                    // we need to see if this one has moved. 
                    if (!string.IsNullOrWhiteSpace(tab))
                    {
                        var tabGroup = item.PropertyGroups.FirstOrDefault(x => x.Name.InvariantEquals(tab));
                        if (tabGroup != null)
                        {
                            if (!tabGroup.PropertyTypes.Contains(property.Alias))
                            {
                                // add to our move list.
                                propertiesToMove[property.Alias] = tab;
                            }
                        }
                    }
                }
            }

            // move things between tabs. 
            MoveProperties(item, propertiesToMove);

            // remove what needs to be removed
            RemoveProperties(item, propertiesNode);
        }

        virtual protected void DeserializeExtraProperties(TObject item, PropertyType property, XElement node)
        {
            // no op.
        }

        protected void DeserializeTabs(TObject item, XElement node)
        {
            logger.Debug(serializerType, "Deserializing Tabs");

            var tabNode = node.Element("Tabs");
            if (tabNode == null) return;

            var defaultSort = 0;

            foreach (var tab in tabNode.Elements("Tab"))
            {
                var name = tab.Element("Caption").ValueOrDefault(string.Empty);
                var sortOrder = tab.Element("SortOrder").ValueOrDefault(defaultSort);

                logger.Debug(serializerType, "> Tab {0} {1}", name, sortOrder);

                var existing = item.PropertyGroups.FirstOrDefault(x => x.Name.InvariantEquals(name));
                if (existing != null)
                {
                    existing.SortOrder = sortOrder;
                }
                else
                {
                    // create the tab
                    if (item.AddPropertyGroup(name))
                    {
                        var newTab = item.PropertyGroups.FirstOrDefault(x => x.Name.InvariantEquals(name));
                        if (newTab != null)
                        {
                            newTab.SortOrder = sortOrder;
                        }
                    }
                }

                defaultSort = sortOrder + 1;
            }
        }


        protected void CleanTabs(TObject item, XElement node)
        {
            logger.Debug(serializerType, "Cleaning Tabs Base");

            var tabNode = node.Element("Tabs");

            if (tabNode == null) return;

            var newTabs = tabNode.Elements("Tab")
                .Where(x => x.Element("Caption") != null)
                .Select(x => x.Element("Caption").ValueOrDefault(string.Empty))
                .ToList();

            List<string> removals = new List<string>();
            foreach (var tab in item.PropertyGroups)
            {
                if (!newTabs.InvariantContains(tab.Name))
                {
                    removals.Add(tab.Name);
                }
            }

            foreach (var name in removals)
            {
                logger.Debug(serializerType, "Removing {0}", name);
                item.PropertyGroups.Remove(name);
            }
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

        protected void DeserializeCompositions(TObject item, XElement node)
        {
            logger.Debug(serializerType, "Deserializing Compositions");

            var comps = node.Element("Info").Element("Compositions");
            if (comps == null) return;
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

            if (!Enumerable.SequenceEqual(item.ContentTypeComposition, compositions))
                item.ContentTypeComposition = compositions;
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


        private void MoveProperties(IContentTypeBase item, IDictionary<string, string> moves)
        {
            logger.Debug(serializerType, "MoveProperties");

            foreach (var move in moves)
            {
                item.MovePropertyType(move.Key, move.Value);
            }
        }

        private void RemoveProperties(IContentTypeBase item, XElement properties)
        {
            logger.Debug(serializerType, "RemoveProperties");

            List<string> removals = new List<string>();

            var nodes = properties.Elements("GenericProperty")
                .Where(x => x.Element("Key") != null)
                .Select(x =>
                    new
                    {
                        Key = x.Element("Key").ValueOrDefault(Guid.Empty),
                        Alias = x.Element("Alias").ValueOrDefault(string.Empty)
                    })
                .ToDictionary(k => k.Key, a => a.Alias);


            foreach (var property in item.PropertyTypes)
            {
                if (nodes.ContainsKey(property.Key)) continue;
                if (nodes.Any(x => x.Value == property.Alias)) continue;
                removals.Add(property.Alias);
            }

            if (removals.Any())
            {
                foreach (var alias in removals)
                {
                    // if you remove something with lots of 
                    // content this can timeout (still? - need to check on v8)
                    logger.Debug(serializerType, "Removing {0}", alias);
                    item.RemovePropertyType(alias);
                }
            }
        }

        #endregion


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

        #endregion

    }
}
