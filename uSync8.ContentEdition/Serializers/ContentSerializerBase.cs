using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

using uSync8.ContentEdition.Mapping;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.ContentEdition.Serializers
{
    public abstract class ContentSerializerBase<TObject> : SyncTreeSerializerBase<TObject>, ISyncContentSerializer<TObject>
        where TObject : IContentBase
    {
        protected UmbracoObjectTypes umbracoObjectType;
        protected SyncValueMapperCollection syncMappers;

        protected ILocalizationService localizationService;
        protected IRelationService relationService;

        protected Dictionary<string, string> pathCache;

        protected string relationAlias;

        public ContentSerializerBase(
            IEntityService entityService,
            ILocalizationService localizationService,
            IRelationService relationService,
            ILogger logger,
            UmbracoObjectTypes umbracoObjectType,
            SyncValueMapperCollection syncMappers)
            : base(entityService, logger)
        {
            this.umbracoObjectType = umbracoObjectType;
            this.syncMappers = syncMappers;

            this.localizationService = localizationService;
            this.relationService = relationService;

            this.pathCache = new Dictionary<string, string>();


        }

        protected virtual XElement InitializeNode(TObject item, string typeName)
        {
            var node = new XElement(this.ItemType,
                new XAttribute("Key", item.Key),
                new XAttribute("Alias", item.Name),
                new XAttribute("Level", GetLevel(item)));

            return node;
        }

        public virtual int GetLevel(TObject item)
        {
            if (!item.Trashed || string.IsNullOrWhiteSpace(relationAlias)) return item.Level;

            // if the item is trashed,then it's level is going to be wrong. 
            // we need to go to the relations service, work out who the parent was,
            // and get the level of that + 1;
            var parent = GetTrashedParent(item);
            if (parent != null)
                return parent.Level + 1;


            return item.Level;
        }

        private IEntitySlim GetTrashedParent(TObject item)
        {
            if (!item.Trashed || string.IsNullOrWhiteSpace(relationAlias)) return null;

            var parents = relationService.GetByChild(item, relationAlias);
            if (parents != null && parents.Any())
            {
                return entityService.Get(parents.FirstOrDefault().ParentId);
            }

            return null;
        }

        protected virtual XElement SerializeInfo(TObject item)
        {
            var info = new XElement("Info");

            var parentKey = Guid.Empty;
            var parentName = "";
            if (item.ParentId != -1)
            {
                var parent = FindItem(item.ParentId);
                if (parent != null)
                {
                    parentKey = parent.Key;
                    parentName = parent.Name;
                }
            }

            info.Add(new XElement("Parent", new XAttribute("Key", parentKey), parentName));
            info.Add(new XElement("Path", GetItemPath(item)));
            info.Add(GetTrashedInfo(item));
            info.Add(new XElement("ContentType", item.ContentType.Alias));
            info.Add(new XElement("CreateDate", item.CreateDate));

            var title = new XElement("NodeName", new XAttribute("Default", item.Name));
            foreach (var culture in item.AvailableCultures)
            {
                title.Add(new XElement("Name", item.GetCultureName(culture),
                    new XAttribute("Culture", culture)));
            }
            info.Add(title);

            info.Add(new XElement("SortOrder", item.SortOrder));

            return info;
        }

        private XElement GetTrashedInfo(TObject item)
        {
            var trashed = new XElement("Trashed", item.Trashed);
            if (item.Trashed)
            {
                var trashedParent = GetTrashedParent(item);
                if (trashedParent != null)
                {
                    trashed.Add(new XAttribute("Parent", trashedParent.Key));
                }
            }
            return trashed;
        }

        protected string[] dontSerialize = new string[] { };

        protected virtual XElement SerializeProperties(TObject item)
        {
            var node = new XElement("Properties");

            foreach (var property in item.Properties
                .Where(x => !dontSerialize.InvariantContains(x.Alias))
                .OrderBy(x => x.Alias))
            {
                var propertyNode = new XElement(property.Alias);

                // this can cause us false change readings
                // but we need to preserve the values if they are blank
                // because we have to be able to set them to blank on deserialization
                foreach (var value in property.Values)
                {
                    var valueNode = new XElement("Value");
                    if (!string.IsNullOrWhiteSpace(value.Culture))
                    {
                        valueNode.Add(new XAttribute("Culture", value.Culture ?? string.Empty));
                    }

                    if (!string.IsNullOrWhiteSpace(value.Segment))
                    {
                        valueNode.Add(new XAttribute("Segment", value.Segment ?? string.Empty));
                    }

                    valueNode.Add(new XCData(GetExportValue(value.EditedValue, property.PropertyType, value.Culture, value.Segment)));
                    // valueNode.Value = value.EditedValue?.ToString() ?? string.Empty;
                    propertyNode.Add(valueNode);
                }

                if (property.Values == null || property.Values.Count == 0)
                {
                    // add a blank one, for change clarity
                    // we do it like this because then it doesn't get collapsed in the XML
                    var emptyValue = new XElement("Value");
                    emptyValue.Add(new XCData(string.Empty));

                    propertyNode.Add(emptyValue);
                }

                node.Add(propertyNode);
            }

            return node;
        }


        protected virtual Attempt<string> DeserializeBase(TObject item, XElement node)
        {
            if (node == null || node.Element("Info") == null) return Attempt.Fail("Missing Node info XML Invalid");
            var info = node.Element("Info");

            var parentId = -1;
            var parentNode = info.Element("Parent");
            if (parentNode != null)
            {
                var parent = FindParent(parentNode, false);
                if (parent == null)
                {
                    var path = info.Element("Path").ValueOrDefault(string.Empty);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        parent = FindParentByPath(path);
                    }
                }

                if (parent != null)
                    parentId = parent.Id;
            }

            if (item.ParentId != parentId)
                item.ParentId = parentId;

            var key = node.GetKey();
            if (key != Guid.Empty && item.Key != key)
                item.Key = key;

            var createDate = info.Element("CreateDate").ValueOrDefault(item.CreateDate);
            if (item.CreateDate != createDate)
                item.CreateDate = createDate;

            DeserializeName(item, node);

            return Attempt.Succeed("Info Deserialized");
        }

        protected Attempt<TObject> DeserializeName(TObject item, XElement node)
        {
            var nameNode = node.Element("Info")?.Element("NodeName");
            if (nameNode == null)
                return Attempt.Fail(item, new Exception("Missing Nodename"));

            var name = nameNode.Attribute("Default").ValueOrDefault(string.Empty);
            if (name != string.Empty)
                item.Name = name;

            foreach (var cultureNode in nameNode.Elements("Name"))
            {
                var culture = cultureNode.Attribute("Culture").ValueOrDefault(string.Empty);
                if (culture == string.Empty) continue;

                var cultureName = cultureNode.ValueOrDefault(string.Empty);
                if (cultureName != string.Empty)
                {
                    item.SetCultureName(cultureName, culture);
                }
            }

            // clear the path cache of anything with this id.
            pathCache.RemoveAll(x => x.Key.Contains(item.Id.ToString()));

            return Attempt.Succeed(item);
        }

        protected Attempt<TObject> DeserializeProperties(TObject item, XElement node)
        {
            var properties = node.Element("Properties");
            if (properties == null || !properties.HasElements)
                return Attempt.Succeed(item); // new Exception("No Properties in the content node"));

            foreach (var property in properties.Elements())
            {
                var alias = property.Name.LocalName;
                if (item.HasProperty(alias))
                {
                    var current = item.Properties[alias];

                    foreach (var value in property.Elements("Value"))
                    {
                        var culture = value.Attribute("Culture").ValueOrDefault(string.Empty);
                        var segment = value.Attribute("Segment").ValueOrDefault(string.Empty);
                        var propValue = value.ValueOrDefault(string.Empty);

                        if (!string.IsNullOrEmpty(culture))
                        {
                            //
                            // check the culture is something we should and can be setting.
                            //
                            if (!current.PropertyType.VariesByCulture())
                            {
                                // if we get here, then things are wrong, so we will try to fix them.
                                //
                                // if the content config thinks it should vary by culture, but the document type doesn't
                                // then we can check if this is default language, and use that to se the value
                                if (!culture.InvariantEquals(localizationService.GetDefaultLanguageIsoCode()))
                                {
                                    // this culture is not the default for the site, so don't use it to 
                                    // set the single language value.
                                    break;
                                }
                                logger.Warn<ContentSerializer>($"Cannot set value on culture {culture} because it is not avalible for this property - value in default language will be used");
                                culture = string.Empty;
                            }
                            else if (!item.AvailableCultures.InvariantContains(culture))
                            {
                                // this culture isn't one of the ones, that can be set on this language. 
                                logger.Warn<ContentSerializer>($"Culture {culture} is not one of the avalible cultures, so we cannot set this value");
                                break;
                            }
                        }
                        var itemValue = GetImportValue(propValue, current.PropertyType, culture, segment);
                        item.SetValue(alias, itemValue, culture, segment);
                    }

                }
            }
            return Attempt.Succeed(item);
        }

        protected void HandleSortOrder(TObject item, int sortOrder)
        {
            if (sortOrder != -1)
                item.SortOrder = sortOrder;
        }

        protected abstract void HandleTrashedState(TObject item, bool trashed);


        protected string GetExportValue(object value, PropertyType propertyType, string culture, string segment)
        {
            // this is where the mapping magic will happen. 
            // at the moment there are no value mappers, but if we need
            // them they plug in as ISyncMapper things
            return syncMappers.GetExportValue(value, propertyType.PropertyEditorAlias);
        }

        protected object GetImportValue(string value, PropertyType propertyType, string culture, string segment)
        {
            // this is where the mapping magic will happen. 
            // at the moment there are no value mappers, but if we need
            // them they plug in as ISyncMapper things
            return syncMappers.GetImportValue(value, propertyType.PropertyEditorAlias);
        }

        public override bool IsValid(XElement node)
             => node != null
                && node.GetKey() != null
                && node.GetAlias() != null
                && node.Element("Info") != null;


        // these are the functions using the simple 'getItem(alias)' 
        // that we cannot use for content/media trees.
        protected override TObject FindOrCreate(XElement node)
        {
            TObject item = FindItem(node);
            if (item != null) return item;

            var alias = node.GetAlias();

            var parentKey = node.Attribute("Parent").ValueOrDefault(Guid.Empty);
            if (parentKey != Guid.Empty)
            {
                item = FindItem(alias, parentKey);
                if (item != null) return item;
            }

            // create
            var parent = default(TObject);

            if (parentKey != Guid.Empty)
            {
                parent = FindItem(parentKey);
            }

            var contentTypeAlias = node.Element("Info").Element("ContentType").ValueOrDefault(node.Name.LocalName);

            // var contentTypeAlias = node.Name.LocalName;

            return CreateItem(alias, parent, contentTypeAlias);
        }

        protected override string GetItemBaseType(XElement node)
            => node.Name.LocalName;

        protected virtual string GetItemPath(TObject item)
        {
            if (pathCache.ContainsKey(item.Path)) return pathCache[item.Path];

            var entity = entityService.Get(item.Id);
            if (entity != null)
                return GetItemPath(entity);

            return "";
        }

        protected virtual string GetItemPath(IEntitySlim item)
        {
            // path caching, stops us looking up the same path all the time.
            if (pathCache.ContainsKey(item.Path)) return pathCache[item.Path];

            var path = "";
            if (item.ParentId != -1)
            {
                var parent = entityService.Get(item.ParentId);
                if (parent != null)
                    path += GetItemPath(parent);
            }

            pathCache[item.Path] = path + "/" + item.Name.ToSafeAlias();
            return pathCache[item.Path];
        }

        public override SyncAttempt<XElement> SerializeEmpty(TObject item, SyncActionType change, string alias)
        {
            var attempt = base.SerializeEmpty(item, change, alias);
            if (attempt.Success)
            {
                attempt.Item.Add(new XAttribute("Level", GetLevel(item)));
            }
            return attempt;
        }

        #region Finders 
        // Finders - used on importing, getting things that are already there (or maybe not)

        public override TObject FindItem(XElement node)
        {
            var (key, alias) = FindKeyAndAlias(node);
            if (key != Guid.Empty)
            {
                var item = FindItem(key);
                if (item != null) return item;
            }

            // else by level 
            var parentKey = node.Attribute("Parent").ValueOrDefault(Guid.Empty);
            if (parentKey != Guid.Empty)
            {
                var item = FindItem(alias, parentKey);
                if (item != null)
                    return item;
            }

            // if we get here, we could try for parent alias, alias ??
            // (really we would need full path e.g home/blog/2019/posts/)
            return default(TObject);
        }

        protected abstract TObject FindItem(int id);

        protected override TObject FindItem(string alias)
        {
            // we can't relaibly do this - because names can be the same
            // across the content treee. but we should have overridden all classes that call this 
            // function above.
            return default(TObject);
        }

        protected virtual TObject FindItem(string alias, Guid parent)
        {
            return FindItem(alias, FindItem(parent));
        }

        protected virtual TObject FindItem(string alias, TObject parent)
        {
            if (parent != null)
            {
                var children = entityService.GetChildren(parent.Id, this.umbracoObjectType);
                var child = children.FirstOrDefault(x => x.Name.ToSafeAlias().InvariantEquals(alias));
                if (child != null)
                    return FindItem(child.Id);
            }
            else
            {
                return FindAtRoot(alias);
            }

            return default(TObject);
        }

        protected abstract TObject FindAtRoot(string alias);

        protected override string ItemAlias(TObject item)
            => item.Name;

        protected TObject FindParent(XElement node, bool searchByAlias = false)
        {
            var item = default(TObject);

            if (node == null) return default(TObject);

            var key = node.Attribute("Key").ValueOrDefault(Guid.Empty);
            if (key != Guid.Empty)
            {
                item = FindItem(key);
                if (item != null) return item;
            }

            if (item == null && searchByAlias)
            {
                var alias = node.ValueOrDefault(string.Empty);
                if (!string.IsNullOrEmpty(alias))
                {
                    item = FindItem(node.ValueOrDefault(alias));
                }
            }

            return item;
        }
        protected TObject FindParentByPath(string path)
        {
            var folders = path.ToDelimitedList("/").ToList();
            return FindByPath(folders.Take(folders.Count - 1));
        }
        protected TObject FindByPath(IEnumerable<string> folders)
        {
            var item = default(TObject);
            foreach (var folder in folders)
            {
                var next = FindItem(folder, item);
                if (next == null)
                    return item;

                item = next;
            }

            return item;

        }



        #endregion
    }
}
