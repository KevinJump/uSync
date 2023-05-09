using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Cache;
using uSync.Core.Mapping;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    public abstract class ContentSerializerBase<TObject> : SyncTreeSerializerBase<TObject>, ISyncContentSerializer<TObject>
        where TObject : IContentBase
    {
        protected UmbracoObjectTypes umbracoObjectType;
        protected SyncValueMapperCollection syncMappers;


        protected readonly IShortStringHelper shortStringHelper;

        protected ILocalizationService localizationService;
        protected IRelationService relationService;

        protected string relationAlias;

        public ContentSerializerBase(
            IEntityService entityService,
            ILocalizationService localizationService,
            IRelationService relationService,
            IShortStringHelper shortStringHelper,
            ILogger<ContentSerializerBase<TObject>> logger,
            UmbracoObjectTypes umbracoObjectType,
            SyncValueMapperCollection syncMappers)
            : base(entityService, logger)
        {
            this.shortStringHelper = shortStringHelper;

            this.umbracoObjectType = umbracoObjectType;
            this.syncMappers = syncMappers;

            this.localizationService = localizationService;
            this.relationService = relationService;
        }

        /// <summary>
        ///  Initialize the XElement with the core Key, Name, Level values
        /// </summary>
        protected virtual XElement InitializeNode(TObject item, string typeName, SyncSerializerOptions options)
        {
            var node = new XElement(this.ItemType,
                new XAttribute(uSyncConstants.Xml.Key, item.Key),
                new XAttribute(uSyncConstants.Xml.Alias, item.Name),
                new XAttribute(uSyncConstants.Xml.Level, GetLevel(item)));

            // are we only serizling some cultures ? 
            var cultures = options.GetSetting(uSyncConstants.CultureKey, string.Empty);
            if (IsPartialCultureElement(item, cultures))
            {
                node.Add(new XAttribute(uSyncConstants.CultureKey, cultures));
            }

            // are we only serializing some segments ? 
            var segments = options.GetSetting(uSyncConstants.SegmentKey, string.Empty);
            if (IsPartialSegmentElement(item, segments))
            {
                node.Add(new XAttribute(uSyncConstants.SegmentKey, segments));
            }

            // are we including the default (not variant) values in the serialized result? 
            // we only worry about this when we are passing partial cultures or segments 
            // to the file, when we sync complete content items, this is redundant. 
            if (options.GetSetting(uSyncConstants.DefaultsKey, true) && IsPartialElement(item, cultures ,segments)) {
                node.Add(new XAttribute(uSyncConstants.DefaultsKey, true));
            }

            return node;
        }

        private bool IsPartialCultureElement(TObject item, string cultures)
            => !string.IsNullOrWhiteSpace(cultures) && item.ContentType.VariesByCulture();

        private bool IsPartialSegmentElement(TObject item, string segments)
            => !string.IsNullOrWhiteSpace(segments) && item.ContentType.VariesBySegment();

        private bool IsPartialElement(TObject item, string cultures, string segments)
            => IsPartialCultureElement(item, cultures) || IsPartialSegmentElement(item, segments);

        /// <summary>
        ///  Calculate the level for this item
        /// </summary>
        /// <remarks>
        ///  Trashed items get a level + 100, so they get processed last
        /// </remarks>
        protected virtual int GetLevel(TObject item)
            => item.Trashed ? 100 + item.Level : item.Level;

        private IEntitySlim GetTrashedParent(TObject item)
        {
            if (!item.Trashed || string.IsNullOrWhiteSpace(relationAlias)) return null;

            var parents = relationService.GetByChild(item, relationAlias);
            if (parents != null && parents.Any())
            {
                return syncMappers.EntityCache.GetEntity(parents.FirstOrDefault().ParentId);
                // return entityService.Get(parents.FirstOrDefault().ParentId);
            }

            return null;
        }

        /// <summary>
        ///  Serialize the Info - (Item Attributes) Node
        /// </summary>
        protected virtual XElement SerializeInfo(TObject item, SyncSerializerOptions options)
        {
            var info = new XElement(uSyncConstants.Xml.Info);

            // find parent. 
            var parentKey = Guid.Empty;
            var parentName = "";
            if (item.ParentId != -1)
            {
                var cachedItem = GetCachedName(item.ParentId);
                if (cachedItem != null)
                {
                    parentKey = cachedItem.Key;
                    parentName = cachedItem.Name;
                }
                else
                {
                    var parent = FindItem(item.ParentId);
                    if (parent != null)
                    {
                        parentKey = parent.Key;
                        parentName = parent.Name;
                    }
                }
            }

            info.Add(new XElement(uSyncConstants.Xml.Parent, new XAttribute(uSyncConstants.Xml.Key, parentKey), parentName));
            info.Add(new XElement(uSyncConstants.Xml.Path, GetItemPath(item)));
            info.Add(GetTrashedInfo(item));
            info.Add(new XElement("ContentType", item.ContentType.Alias));
            info.Add(new XElement("CreateDate", item.CreateDate.ToString("s")));

            var cultures = options.GetCultures();

            var title = new XElement("NodeName", new XAttribute("Default", item.Name));
            foreach (var culture in item.AvailableCultures.OrderBy(x => x))
            {
                if (cultures.IsValidOrBlank(culture))
                {
                    title.Add(new XElement(uSyncConstants.Xml.Name, item.GetCultureName(culture),
                        new XAttribute("Culture", culture)));
                }
            }
            info.Add(title);

            info.Add(new XElement(uSyncConstants.Xml.SortOrder, item.SortOrder));

            return info;
        }

        /// <summary>
        ///  get the trash information (including non-trashed parent)
        /// </summary>
        private XElement GetTrashedInfo(TObject item)
        {
            var trashed = new XElement("Trashed", item.Trashed);
            if (item.Trashed)
            {
                var trashedParent = GetTrashedParent(item);
                if (trashedParent != null)
                {
                    trashed.Add(new XAttribute(uSyncConstants.Xml.Parent, trashedParent.Key));
                }
            }
            return trashed;
        }

        /// <summary>
        ///  Things not to serialize (mediaSerializer overrides this, for Auto properties)
        /// </summary>
        protected string[] dontSerialize = new string[] { };

        /// <summary>
        ///  serialize all the properties for the item
        /// </summary>
        protected virtual XElement SerializeProperties(TObject item, SyncSerializerOptions options)
        {
            var cultures = options.GetCultures();
            var segments = options.GetSegments();
            var includeDefaults = (cultures.Count == 0 && segments.Count == 0)
                || options.GetSetting(uSyncConstants.DefaultsKey, false);

            var excludedProperties = GetExcludedProperties(options);
            var excludedPropertyPatterns = GetExcludedPropertyPatterns(options);
            var availableCultures = item.AvailableCultures.ToList();

            var node = new XElement("Properties");

            foreach (var property in item.Properties
                .Where(x => !excludedProperties.InvariantContains(x.Alias))
                .Where(x => !excludedPropertyPatterns.Any(y => y.IsMatch(x.Alias)))
                .OrderBy(x => x.Alias))
            {
                var elements = new List<XElement>();

                // this can cause us false change readings
                // but we need to preserve the values if they are blank
                // because we have to be able to set them to blank when we deserialize them.
                foreach (var value in property.Values.OrderBy(x => x.Culture ?? ""))
                {
                    var valueNode = new XElement("Value");

                    // valid if there is no culture, or segment and 
                    // we are including default values
                    var validNode = string.IsNullOrWhiteSpace(value.Culture)
                        && string.IsNullOrWhiteSpace(value.Segment)
                        && includeDefaults;


                    // or b) it is a valid culture/segment. 
                    if (!string.IsNullOrWhiteSpace(value.Culture) && cultures.IsValid(value.Culture))
                    {
                        valueNode.Add(new XAttribute("Culture", value.Culture ?? string.Empty));
                        validNode = true;
                    }


                    if (!string.IsNullOrWhiteSpace(value.Segment) && segments.IsValid(value.Segment))
                    {
                        valueNode.Add(new XAttribute("Segment", value.Segment ?? string.Empty));
                        validNode = true;
                    }

                    if (validNode)
                    {
                        valueNode.Add(new XCData(GetExportValue(GetPropertyValue(value), property.PropertyType, value.Culture, value.Segment)));
                        elements.Add(valueNode);
                    }
                }


                if (includeDefaults)
                {
                    if (property.PropertyType.VariesByCulture())
                    {
                        foreach (var culture in availableCultures)
                        {
                            if (!cultures.IsValid(culture)) continue;

                            // add a blank value for any missing culture values.
                            if (!property.Values.Any(x => (x.Culture ?? "").Equals(culture, StringComparison.OrdinalIgnoreCase)))
                            {
                                elements.Add(new XElement("Value",
                                    new XAttribute("Culture", culture),
                                    new XCData(string.Empty)));
                            }
                        }
                    }
                    else if (property.Values == null || property.Values.Count == 0)
                    {
                        // add a blank one, for change clarity
                        // we do it like this because then it doesn't get collapsed in the XML serialization
                        elements.Add(new XElement("Value",
                            new XCData(string.Empty)));

                    }
                }

                if (elements.Count > 0)
                {
                    // we sort them at the end because we might end up adding a blank culture value last. 
                    var propertyNode = new XElement(property.Alias);
                    propertyNode.Add(elements.OrderBy(x => x.Attribute("Culture").ValueOrDefault("")));
                    node.Add(propertyNode);
                }
            }

            return node;
        }

        // allows us to switch between published / edited easier.
        protected virtual object GetPropertyValue(IPropertyValue value)
            => value.EditedValue;

        protected override SyncAttempt<TObject> CanDeserialize(XElement node, SyncSerializerOptions options)
        {
            if (options.FailOnMissingParent)
            {
                // check the parent exists. 
                if (!this.HasParentItem(node))
                {
                    return SyncAttempt<TObject>.Fail(node.GetAlias(), ChangeType.ParentMissing, $"The parent node for this item is missing, and configuration is set to not import when a parent is missing");

                }
            }
            return SyncAttempt<TObject>.Succeed("No check", ChangeType.NoChange);
        }

        protected virtual IEnumerable<uSyncChange> DeserializeBase(TObject item, XElement node, SyncSerializerOptions options)
        {
            var info = node?.Element(uSyncConstants.Xml.Info);
            if (info == null) return Enumerable.Empty<uSyncChange>();

            var changes = new List<uSyncChange>();


            var trashed = info.Element("Trashed").ValueOrDefault(false);

            if (!trashed)
            {
                // only try and set the path if the item isn't trashed. 

                var parentId = -1;
                var nodeLevel = CalculateNodeLevel(item, default(TObject));
                var nodePath = CalculateNodePath(item, default(TObject));

                var parentNode = info.Element(uSyncConstants.Xml.Parent);
                if (parentNode != null)
                {
                    var parent = FindParent(parentNode, false);
                    if (parent == null)
                    {
                        var friendlyPath = info.Element(uSyncConstants.Xml.Path).ValueOrDefault(string.Empty);
                        if (!string.IsNullOrWhiteSpace(friendlyPath))
                        {
                            logger.LogDebug("Find Parent failed, will search by path {FriendlyPath}", friendlyPath);
                            parent = FindParentByPath(friendlyPath);
                        }
                    }

                    if (parent != null)
                    {
                        parentId = parent.Id;
                        nodePath = CalculateNodePath(item, parent);
                        nodeLevel = CalculateNodeLevel(item, parent);
                    }
                    else
                    {
                        logger.LogDebug("Unable to find parent but parent node is set in configuration");
                    }
                }

                if (item.ParentId != parentId)
                {
                    changes.AddUpdate(uSyncConstants.Xml.Parent, item.ParentId, parentId);
                    logger.LogTrace("{Id} Setting Parent {ParentId}", item.Id, parentId);
                    item.ParentId = parentId;
                }

                // the following are calculated (not in the file
                // because they might change without this node being saved).
                if (item.Path != nodePath)
                {
                    changes.AddUpdate(uSyncConstants.Xml.Path, item.Path, nodePath);
                    logger.LogDebug("{Id} Setting Path {idPath} was {oldPath}", item.Id, nodePath, item.Path);
                    item.Path = nodePath;
                }

                if (item.Level != nodeLevel)
                {
                    changes.AddUpdate(uSyncConstants.Xml.Level, item.Level, nodeLevel);
                    logger.LogDebug("{Id} Setting Level to {Level} was {OldLevel}", item.Id, nodeLevel, item.Level);
                    item.Level = nodeLevel;
                }
            }


            var key = node.GetKey();
            if (key != Guid.Empty && item.Key != key)
            {
                changes.AddUpdate(uSyncConstants.Xml.Key, item.Key, key);
                logger.LogTrace("{Id} Setting Key {Key}", item.Id, key);
                item.Key = key;
            }

            var createDate = info.Element("CreateDate").ValueOrDefault(item.CreateDate);
            if (item.CreateDate != createDate)
            {
                changes.AddUpdate("CreateDate", item.CreateDate, createDate);
                logger.LogDebug("{id} Setting CreateDate: {createDate}", item.Id, createDate);
                item.CreateDate = createDate;
            }

            changes.AddRange(DeserializeName(item, node, options));

            return changes;
        }

        protected IEnumerable<uSyncChange> DeserializeName(TObject item, XElement node, SyncSerializerOptions options)
        {
            var nameNode = node.Element(uSyncConstants.Xml.Info)?.Element("NodeName");
            if (nameNode == null)
                return Enumerable.Empty<uSyncChange>();

            var updated = false;


            var changes = new List<uSyncChange>();

            var name = nameNode.Attribute("Default").ValueOrDefault(string.Empty);
            if (name != string.Empty && item.Name != name)
            {
                changes.AddUpdate(uSyncConstants.Xml.Name, item.Name, name);
                updated = true;

                item.Name = name;
            }

            if (nameNode.HasElements)
            {
                var activeCultures = options.GetDeserializedCultures(node);

                foreach (var cultureNode in nameNode.Elements(uSyncConstants.Xml.Name))
                {
                    var culture = cultureNode.Attribute("Culture").ValueOrDefault(string.Empty);
                    if (culture == string.Empty) continue;

                    if (activeCultures.IsValid(culture))
                    {

                        var cultureName = cultureNode.ValueOrDefault(string.Empty);
                        var currentCultureName = item.GetCultureName(culture);
                        if (cultureName != string.Empty && cultureName != currentCultureName)
                        {
                            changes.AddUpdate($"Name ({culture})", currentCultureName, cultureName);
                            updated = true;

                            item.SetCultureName(cultureName, culture);
                        }
                    }
                }
            }

            if (updated) CleanCaches(item.Id);

            return changes;
        }

        protected Attempt<List<uSyncChange>, string> DeserializeProperties(TObject item, XElement node, SyncSerializerOptions options)
        {
            string errors = "";
            List<uSyncChange> changes = new List<uSyncChange>();

            var activeCultures = options.GetDeserializedCultures(node);

            var properties = node.Element("Properties");
            if (properties == null || !properties.HasElements)
                return Attempt.SucceedWithStatus(errors, changes); // new Exception("No Properties in the content node"));

            foreach (var property in properties.Elements())
            {
                var alias = property.Name.LocalName;
                if (item.HasProperty(alias))
                {
                    var current = item.Properties[alias];

                    logger.LogTrace("De-serialize Property {0} {1}", alias, current.PropertyType.PropertyEditorAlias);

                    var values = property.Elements("Value").ToList();

                    foreach (var value in values)
                    {
                        var culture = value.Attribute("Culture").ValueOrDefault(string.Empty);
                        var segment = value.Attribute("Segment").ValueOrDefault(string.Empty);
                        var propValue = value.ValueOrDefault(string.Empty);

                        logger.LogTrace("{item} {Property} Culture {Culture} Segment {Segment}", item.Name, alias, culture, segment);

                        try
                        {
                            if (!string.IsNullOrEmpty(culture) && activeCultures.IsValid(culture))
                            {
                                //
                                // check the culture is something we should and can be setting.
                                //
                                if (!current.PropertyType.VariesByCulture())
                                {
                                    logger.LogTrace("Item does not vary by culture - but uSync item file contains culture");
                                    // if we get here, then things are wrong, so we will try to fix them.
                                    //
                                    // if the content config thinks it should vary by culture, but the document type doesn't
                                    // then we can check if this is default language, and use that to se the value
                                    if (!culture.InvariantEquals(localizationService.GetDefaultLanguageIsoCode()))
                                    {
                                        // this culture is not the default for the site, so don't use it to 
                                        // set the single language value.
                                        logger.LogWarning("{item} Culture {culture} in file, but is not default so not being used", item.Name, culture);
                                        continue;
                                    }
                                    logger.LogWarning("{item} Cannot set value on culture {culture} because it is not available for this property - value in default language will be used", item.Name, culture);
                                    culture = string.Empty;
                                }
                                else if (!item.AvailableCultures.InvariantContains(culture))
                                {
                                    // this culture isn't one of the ones, that can be set on this language. 
                                    logger.LogWarning("{item} Culture {culture} is not one of the available cultures, so we cannot set this value", item.Name, culture);
                                    continue;
                                }
                            }
                            else
                            {
                                // no culture, but we have to check, because if the property now varies by culture, this can have a random no-cultured value in it?
                                if (current.PropertyType.VariesByCulture())
                                {

                                    if (values.Count == 1)
                                    {
                                        // there is only one value - so we should set the default variant with this for consistency?
                                        culture = localizationService.GetDefaultLanguageIsoCode();
                                        logger.LogDebug("Property {Alias} contains a single value that has no culture setting default culture {Culture}", alias, culture);
                                    }
                                    else
                                    {
                                        logger.LogDebug("{item} Property {Alias} contains a value that has no culture but this property varies by culture so this value has no effect", item.Name, alias);
                                        continue;
                                    }
                                }
                            }

                            // get here ... set the value
                            var itemValue = GetImportValue(propValue, current.PropertyType, culture, segment);
                            var currentValue = item.GetValue(alias, culture, segment);

                            if (IsUpdatedValue(currentValue, itemValue))
                            {
                                changes.AddUpdateJson(alias, currentValue, itemValue, $"Property/{alias}");

                                item.SetValue(alias, itemValue,
                                    string.IsNullOrEmpty(culture) ? null : culture,
                                    string.IsNullOrEmpty(segment) ? null : segment);

                                logger.LogDebug("Property [{id}] {item} set {alias} value", item.Id, item.Name, alias);
                            }
                        }
                        catch (Exception ex)
                        {
                            // capture here to be less aggressive with failure. 
                            // if one property fails the rest will still go in.
                            logger.LogWarning("{item} Failed to set [{alias}] {propValue} Ex: {Exception}", item.Name, alias, propValue, ex.ToString());
                            errors += $"Failed to set [{alias}] {ex.Message} <br/>";
                        }
                    }
                }
                else
                {
                    logger.LogWarning("DeserializeProperties: item {Name} doesn't have property '{alias}' but its in the XML", item.Name, alias);
                    errors += $"{item.Name} does not container property {alias}";
                    changes.Add(uSyncChange.Warning($"Property/{alias}", alias, $"item {item.Name} does not have a property {alias} but it exists in XML"));
                }
            }

            return Attempt.SucceedWithStatus(errors, changes);
        }

        /// <summary>
        ///  compares to object values to see if they are the same. 
        /// </summary>
        /// <remarks>
        ///   Object.Equals will check nulls, and object values. but 
        ///   the value from the XML will not be coming back as the 
        ///   same type as that in the object (if its set). 
        ///   
        ///   So we attempt to convert to the type stored in the current
        ///   value, and then compare that. which gets us a better check.
        /// </remarks>
        private bool IsUpdatedValue(object current, object newValue)
        {
            if (Object.Equals(current, newValue)) return false;

            // different types? 
            if (current != null && newValue != null && current.GetType() != newValue.GetType())
            {
                var currentType = current.GetType();
                var attempt = newValue.TryConvertTo(currentType);
                if (attempt.Success) return !current.Equals(attempt.Result);
            }

            return true;
        }


        protected uSyncChange HandleSortOrder(TObject item, int sortOrder)
        {
            if (sortOrder != -1 && item.SortOrder != sortOrder)
            {
                logger.LogTrace("{id} Setting Sort Order {sortOrder}", item.Id, sortOrder);

                var currentSortOrder = item.SortOrder;

                item.SortOrder = sortOrder;

                return uSyncChange.Update(uSyncConstants.Xml.SortOrder, uSyncConstants.Xml.SortOrder, currentSortOrder, sortOrder);
            }

            return null;
        }

        protected abstract uSyncChange HandleTrashedState(TObject item, bool trashed);

        protected string GetExportValue(object value, IPropertyType propertyType, string culture, string segment)
        {
            // this is where the mapping magic will happen. 
            // at the moment there are no value mappers, but if we need
            // them they plug in as ISyncMapper things
            logger.LogTrace("Getting ExportValue [{PropertyEditorAlias}]", propertyType.PropertyEditorAlias);

            var exportValue = syncMappers.GetExportValue(value, propertyType.PropertyEditorAlias);

            // TODO: in a perfect world, this is the best answer, don't escape any buried JSON in anything
            // but there might be a couple of property value converters that don't like their nested JSON
            // to not be escaped so we would need to do proper testing. 
            if (exportValue.DetectIsJson() && !exportValue.IsAngularExpression())
            {
                var tokenValue = exportValue.GetJsonTokenValue().ExpandAllJsonInToken();
                return JsonConvert.SerializeObject(tokenValue, Formatting.Indented);
            }
            logger.LogTrace("Export Value {PropertyEditorAlias} {exportValue}", propertyType.PropertyEditorAlias, exportValue);
            return exportValue;
        }

        protected object GetImportValue(string value, IPropertyType propertyType, string culture, string segment)
        {
            // this is where the mapping magic will happen. 
            // at the moment there are no value mappers, but if we need
            // them they plug in as ISyncMapper things
            logger.LogTrace("Getting ImportValue [{PropertyEditorAlias}]", propertyType.PropertyEditorAlias);

            var importValue = syncMappers.GetImportValue(value, propertyType.PropertyEditorAlias);
            logger.LogTrace("Import Value {PropertyEditorAlias} {importValue}", propertyType.PropertyEditorAlias, importValue);
            return importValue;
        }

        /// <summary>
        ///  validate that the node is valid
        /// </summary>
        public override bool IsValid(XElement node)
             => node != null
                && node.GetAlias() != null
                && node.Element(uSyncConstants.Xml.Info) != null;


        // these are the functions using the simple 'getItem(alias)' 
        // that we cannot use for content/media trees.
        protected override Attempt<TObject> FindOrCreate(XElement node)
        {
            TObject item = FindItem(node);
            if (item != null) return Attempt.Succeed(item);

            var alias = node.GetAlias();

            var parentKey = node.Attribute(uSyncConstants.Xml.Parent).ValueOrDefault(Guid.Empty);
            if (parentKey != Guid.Empty)
            {
                item = FindItem(alias, parentKey);
                if (item != null) return Attempt.Succeed(item);
            }

            // create
            var parent = default(TObject);

            if (parentKey != Guid.Empty)
            {
                parent = FindItem(parentKey);
            }

            var contentTypeAlias = node.Element(uSyncConstants.Xml.Info).Element("ContentType").ValueOrDefault(node.Name.LocalName);

            // var contentTypeAlias = node.Name.LocalName;

            return CreateItem(alias, parent, contentTypeAlias);
        }

        protected override string GetItemBaseType(XElement node)
            => node.Name.LocalName;

        public virtual string GetItemPath(TObject item) => GetFriendlyPath(item.Path);

        /// <summary>
        ///  Get the friendly path for an item, leaning on our internal cache
        ///  as best we can.
        /// </summary>
        /// <remarks>
        ///  The path is a list of ids, e.g -1,1024,1892,2094,4811
        ///  
        ///  for speed we cache path lookups so we don't have to do them again, 
        ///  (e.g -1,1024,1892) - 
        ///  
        ///  so when we get a path from a node, we want to find the largest 
        ///  string of ids that is cached, and then we will have to lookup 
        ///  the remainder, 
        ///  
        ///  we use to do this by recursing down, but entityService.GetAll is 
        ///  faster then individual calls to Get - so its quicker to do it
        ///  in a batch, as long as we don't ask for the ones we already know 
        ///  about. 
        /// </remarks>
        private string GetFriendlyPath(string path)
        {
            try
            {
                var ids = path.ToDelimitedList().Select(x => int.Parse(x, CultureInfo.InvariantCulture));
                var lookups = new List<int>();
                var friendlyPath = "";

                foreach (var id in ids.Where(x => x != -1))
                {
                    var cachedItem = GetCachedName(id);
                    if (cachedItem == null)
                    {
                        lookups.Add(id);
                        friendlyPath += $"/[{id}]";
                    }
                    else
                    {
                        friendlyPath += "/" +  cachedItem.Name.ToSafeAlias(shortStringHelper);
                    }
                }

                var items = syncMappers.EntityCache.GetAll(this.umbracoObjectType, lookups.ToArray());
                // var items = entityService.GetAll(this.umbracoObjectType, lookups.ToArray());
                foreach (var item in items)
                {
                    AddToNameCache(item.Id, item.Key, item.Name);
                    friendlyPath = friendlyPath.Replace($"[{item.Id}]", item.Name.ToSafeAlias(shortStringHelper));
                }

                return friendlyPath;
            }
            catch(Exception ex)
            {
                logger.LogWarning(ex, "Unable to parse path {path}", path);
                return path;
            }
        }

        public override SyncAttempt<XElement> SerializeEmpty(TObject item, SyncActionType change, string alias)
        {
            var attempt = base.SerializeEmpty(item, change, alias);
            if (attempt.Success)
            {
                attempt.Item.Add(new XAttribute(uSyncConstants.Xml.Level, GetLevel(item)));
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
            var parentKey = node.Attribute(uSyncConstants.Xml.Parent).ValueOrDefault(Guid.Empty);
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

        public override TObject FindItem(string alias)
        {
            // For content based items we can't reliably do this - because names can be the same
            // across the content tree. but we should have overridden all classes that call this 
            // function above.
            return default(TObject);
        }

        protected virtual TObject FindItem(string alias, Guid parentKey)
        {
            var parentItem = FindItem(parentKey);
            if (parentItem != null)
            {
                return FindItem(alias, parentItem);
            }
            else if (parentKey == Guid.Empty)
            {
                FindAtRoot(alias);
            }

            return default;
        }

        protected virtual TObject FindItem(string alias, TObject parent)
        {
            if (parent != null)
            {
                var children = entityService.GetChildren(parent.Id, this.umbracoObjectType);
                var child = children.FirstOrDefault(x => x.Name.ToSafeAlias(shortStringHelper).InvariantEquals(alias));
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

        public override string ItemAlias(TObject item)
            => item.Name;

        protected TObject FindParent(XElement node, bool searchByAlias = false)
        {
            var item = default(TObject);

            if (node == null) return default(TObject);

            var key = node.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);
            if (key != Guid.Empty)
            {
                logger.LogTrace("Looking for Parent by Key {Key}", key);
                item = FindItem(key);
                if (item != null) return item;
            }

            if (item == null && searchByAlias)
            {
                var alias = node.ValueOrDefault(string.Empty);
                logger.LogTrace("Looking for Parent by Alias {Alias}", alias);
                if (!string.IsNullOrEmpty(alias))
                {
                    item = FindItem(node.ValueOrDefault(alias));
                }
            }

            return item;
        }
        protected TObject FindParentByPath(string path, bool failIfNotExact = false)
        {
            // logger.Debug(serializerType, "Looking for Parent by path {Path}", path);
            var folders = path.ToDelimitedList("/").ToList();
            return FindByPath(folders.Take(folders.Count - 1), failIfNotExact);
        }
        protected TObject FindByPath(IEnumerable<string> folders, bool failIfNotExact)
        {
            var item = default(TObject);
            foreach (var folder in folders)
            {
                logger.LogTrace("Looking for Item in folder {folder}", folder);
                var next = FindItem(folder, item);
                if (next == null)
                {
                    // if we get lost 1/2 way we are returning that as the path? which would put us in an odd place?
                    logger.LogTrace("Didn't find {folder} returning last found Parent", folder);

                    // if we don't fail on exact this is OK, 
                    // else its not - so we haven't 'found' the right place.
                    return !failIfNotExact ? item : default;
                }

                item = next;
            }

            if (item == null)
            {
                logger.LogDebug("Parent not found in the path");
            }
            else
            {
                logger.LogTrace("Parent Item Found {Name} {id}", item.Name, item.Id);
            }

            return item;
        }



        #endregion

        /// <summary>
        ///  will check the XML to see if the specified parent exists in umbraco
        /// </summary>
        /// <remarks>
        ///  Will first look for the parent based on the key, if this fails
        ///  we look based on friendly path, which might help.
        /// </remarks>
        protected override bool HasParentItem(XElement node)
        {
            var info = node.Element(uSyncConstants.Xml.Info);
            var parentNode = info?.Element(uSyncConstants.Xml.Parent);
            if (parentNode == null) return true;

            if (parentNode.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty) == Guid.Empty) return true;

            var parent = FindParent(parentNode, false);
            if (parent == null)
            {
                var friendlyPath = info.Element(uSyncConstants.Xml.Path).ValueOrDefault(string.Empty);
                if (!string.IsNullOrWhiteSpace(friendlyPath))
                {
                    parent = FindParentByPath(friendlyPath, true);
                }
            }

            return parent != null;
        }

        private void CleanCaches(int id)
        {
            // clean the name cache for this id.
            // nameCache.Remove(id);
        }

        protected CachedName GetCachedName(int id)
                  => syncMappers.EntityCache.GetName(id);

        protected void AddToNameCache(int id, Guid key, string name)
            => syncMappers.EntityCache.AddName(id, key, name);



        /// <summary>
        ///  Remove relations from the 'OnDelete' relation tables. 
        /// </summary>
        /// <remarks>
        ///  While we do move the content/media back, it doesn't always clean the relations table.
        /// </remarks>
        protected void CleanRelations(TObject item, string relationType)
        {
            try
            {
                // clean them up here.
                var deleteRelations = relationService.GetByChild(item, relationType);
                if (deleteRelations.Any())
                {
                    foreach (var deleteRelation in deleteRelations)
                    {
                        relationService.Delete(deleteRelation);
                    }
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Error cleaning up relations: {id}", item.Id);
            }

        }


        private List<string> GetExcludedProperties(SyncSerializerOptions options)
        {
            List<string> exclude = new List<string>(dontSerialize);
            var excludeOptions = options.GetSetting<string>("DoNotSerialize", "");
            if (!string.IsNullOrWhiteSpace(excludeOptions))
                exclude.AddRange(excludeOptions.ToDelimitedList());

            return exclude;
        }

        private List<Regex> GetExcludedPropertyPatterns(SyncSerializerOptions options)
        {
            const string settingsKey = "DoNotSerializePatterns";

            List<Regex> exclude = new List<Regex>();
            string excludeOptions = options.GetSetting<string>(settingsKey, "");
            if (!string.IsNullOrWhiteSpace(excludeOptions))
            {
                foreach (string pattern in excludeOptions.ToDelimitedList())
                {
                    try
                    {
                        exclude.Add(new Regex(pattern));
                    }
                    catch (ArgumentException ex)
                    {
                        logger.LogDebug("Unable to parse pattern '{pattern}' from '{settingsKey}' as Regex. {error}. Pattern will not be considered.", pattern, settingsKey, ex.Message);
                    }
                }
            }

            return exclude;
        }


    }
}
