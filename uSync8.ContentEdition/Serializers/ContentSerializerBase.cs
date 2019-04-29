﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.Core;
using uSync8.Core.Extensions;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.ContentEdition.Serializers
{
    public abstract class ContentSerializerBase<TObject> : SyncTreeSerializerBase<TObject>
        where TObject : IContentBase
    {

        protected UmbracoObjectTypes umbracoObjectType;

        public ContentSerializerBase(IEntityService entityService, ILogger logger, UmbracoObjectTypes umbracoObjectType)
            : base(entityService, logger)
        {
            this.umbracoObjectType = umbracoObjectType;
        }

        protected virtual XElement InitializeNode(TObject item, string typeName)
        {
            var node = new XElement(typeName,
                new XAttribute("Key", item.Key),
                new XAttribute("Alias", item.Name),
                new XAttribute("Level", item.Level));

            return node;
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

        protected virtual XElement SerializeProperties(TObject item)
        {
            var node = new XElement("Properties");

            foreach (var property in item.Properties.OrderBy(x => x.Alias))
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

                    valueNode.Value = value.EditedValue?.ToString() ?? string.Empty;
                    propertyNode.Add(valueNode);
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

            return Attempt.Succeed(item);
        }

        protected Attempt<TObject> DeserializeProperties(TObject item, XElement node)
        {
            var properties = node.Element("Properties");
            if (properties == null || !properties.HasElements)
                return Attempt.Fail(item, new Exception("No Properties in the content node"));

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

                        var itemValue = GetImportValue(current.PropertyType, propValue, culture, segment);
                        item.SetValue(alias, itemValue, culture, segment);
                    }

                }
            }

            return Attempt.Succeed(item);
        }

        protected string GetExportValue(PropertyType propertyType, object value, string culture, string segment)
        {
            // this is where the mapping magic will happen. 
            return (string)value;
        }

        protected string GetImportValue(PropertyType propertyType, string value, string culture, string segment)
        {
            // this is where the mapping magic will happen. 
            return value;
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

            var contentTypeAlias = node.Name.LocalName;

            return CreateItem(alias, parent, contentTypeAlias);
        }

        protected override string GetItemBaseType(XElement node)
            => node.Name.LocalName;

        protected virtual string GetItemPath(TObject item)
        {
            var entity = entityService.Get(item.Id);
            return GetItemPath(entity);
        }

        protected virtual string GetItemPath(IEntitySlim item)
        {
            var path = "";
            if (item.ParentId != -1)
            {
                var parent = entityService.Get(item.ParentId);
                if (parent != null)
                    path += GetItemPath(parent);
            }

            return path += "/" + item.Name.ToSafeAlias();
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
