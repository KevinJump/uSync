using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers
{
    [SyncSerializer("B3F7F247-6077-406D-8480-DB1004C8211C", "ContentTypeSerializer", uSyncConstants.Serialization.ContentType)]
    public class ContentTypeSerializer : ContentTypeBaseSerializer<IContentType>, ISyncSerializer<IContentType>
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IFileService _fileService;

        private readonly uSyncCapabilityChecker _capabilities;

        public ContentTypeSerializer(
            IEntityService entityService, ILogger<ContentTypeSerializer> logger,
            IDataTypeService dataTypeService,
            IContentTypeService contentTypeService,
            IFileService fileService,
            IShortStringHelper shortStringHelper,
            AppCaches appCaches,
            uSyncCapabilityChecker uSyncCapabilityChecker)
            : base(entityService, logger, dataTypeService, contentTypeService, UmbracoObjectTypes.DocumentTypeContainer, shortStringHelper, appCaches, contentTypeService)
        {
            this._contentTypeService = contentTypeService;
            this._fileService = fileService;
            _capabilities = uSyncCapabilityChecker;
        }

        protected override SyncAttempt<XElement> SerializeCore(IContentType item, SyncSerializerOptions options)
        {
            var node = SerializeBase(item);
            var info = SerializeInfo(item);

            var history = SerializeCleanupHistory(item);
            if (history != null) info.Add(history);

            var parent = item.ContentTypeComposition.FirstOrDefault(x => x.Id == item.ParentId);
            if (parent != null)
            {
                info.Add(new XElement(uSyncConstants.Xml.Parent, parent.Alias,
                            new XAttribute(uSyncConstants.Xml.Key, parent.Key)));
            }
            else if (item.Level != 1)
            {
                var folderNode = this.GetFolderNode(item);
                if (folderNode != null)
                    info.Add(folderNode);
            }

            // compositions ? 
            info.Add(SerializeCompostions((ContentTypeCompositionBase)item));

            // templates
            var templateAlias =
                (item.DefaultTemplate != null && item.DefaultTemplate.Id != 0)
                ? item.DefaultTemplate.Alias
                : "";

            info.Add(new XElement("DefaultTemplate", templateAlias));

            var templates = SerailizeTemplates(item);
            if (templates != null)
                info.Add(templates);

            node.Add(info);
            node.Add(SerializeStructure(item));
            node.Add(SerializeProperties(item));
            node.Add(SerializeTabs(item));

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IContentType), ChangeType.Export);
        }

        protected override void SerializeExtraProperties(XElement node, IContentType item, IPropertyType property)
        {
            node.Add(new XElement("Variations", property.Variations));
        }

        private XElement SerailizeTemplates(IContentType item)
        {
            var node = new XElement("AllowedTemplates");
            if (item.AllowedTemplates != null && item.AllowedTemplates.Any())
            {
                foreach (var template in item.AllowedTemplates.OrderBy(x => x.Alias))
                {
                    node.Add(new XElement("Template", template.Alias,
                        new XAttribute(uSyncConstants.Xml.Key, template.Key)));
                }
            }

            return node;
        }

        protected override SyncAttempt<IContentType> DeserializeCore(XElement node, SyncSerializerOptions options)
        {
            var attempt = FindOrCreate(node);
            if (!attempt.Success) throw attempt.Exception;

            var item = attempt.Result;

            var details = new List<uSyncChange>();

            details.AddRange(DeserializeBase(item, node));
            details.AddRange(DeserializeTabs(item, node));
            details.AddRange(DeserializeProperties(item, node, options));

            // content type only property stuff.
            details.AddRange(DeserializeContentTypeProperties(item, node));

            // templates 
            details.AddRange(DeserializeTemplates(item, node));

            return DeserializedResult(item, details, options);
        }

        protected override IEnumerable<uSyncChange> DeserializeExtraProperties(IContentType item, IPropertyType property, XElement node)
        {
            var variations = node.Element("Variations").ValueOrDefault(ContentVariation.Nothing);
            if (property.Variations != variations)
            {
                var change = uSyncChange.Update("Property/Variations", "Variations", property.Variations, variations);

                property.Variations = variations;

                return change.AsEnumerableOfOne();
            }

            return Enumerable.Empty<uSyncChange>();
        }

        public override SyncAttempt<IContentType> DeserializeSecondPass(IContentType item, XElement node, SyncSerializerOptions options)
        {
            logger.LogDebug("Deserialize Second Pass {0}", item.Alias);

            var details = new List<uSyncChange>();

            SetSafeAliasValue(item, node, false);

            details.AddRange(DeserializeCompositions(item, node));
            details.AddRange(DeserializeStructure(item, node));

            // When doing this reflectiony - it doesn't set is dirty. 
            var historyChanges = DeserializeCleanupHistory(item, node);
            var historyUpdated = historyChanges.Any(x => x.Change > ChangeDetailType.NoChange);
            details.AddRange(historyChanges);


            CleanTabAliases(item);

            // clean tabs 
            details.AddRange(CleanTabs(item, node, options));

            bool saveInSerializer = !options.Flags.HasFlag(SerializerFlags.DoNotSave);
            if (saveInSerializer && (item.IsDirty() || historyUpdated))
            {
                var dirty = string.Join(", ", item.GetDirtyProperties());
                dirty += string.Join(", ", item.PropertyGroups.Where(x => x.IsDirty()).Select(x => $"Group:{x.Name}"));
                dirty += string.Join(", ", item.PropertyTypes.Where(x => x.IsDirty()).Select(x => $"Property:{x.Name}"));
                dirty += historyUpdated ? " CleanupHistory" : "";
                logger.LogDebug("Saving in Serializer because item is dirty [{properties}]", dirty);

                _contentTypeService.Save(item);
            }

            CleanFolder(item, node);

            return SyncAttempt<IContentType>.Succeed(item.Name, item, ChangeType.Import, "", saveInSerializer, details);
        }

        private IEnumerable<uSyncChange> DeserializeContentTypeProperties(IContentType item, XElement node)
        {
            var info = node?.Element("Info");
            if (info == null) return Enumerable.Empty<uSyncChange>();

            var changes = new List<uSyncChange>();

            var isContainer = info.Element("IsListView").ValueOrDefault(false);
            if (item.IsContainer != isContainer)
            {
                changes.AddUpdate("IsListView", item.IsContainer, isContainer, "Info/IsListView");
                item.IsContainer = isContainer;
            }

            var masterTemplate = info.Element("DefaultTemplate").ValueOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(masterTemplate))
            {
                var template = _fileService.GetTemplate(masterTemplate);
                if (template != null)
                {
                    if (item.DefaultTemplate == null || template.Alias != item.DefaultTemplate.Alias)
                    {
                        changes.AddUpdate("DefaultTemplate", item.DefaultTemplate?.Alias ?? string.Empty, masterTemplate, "DefaultTemplate");
                        item.SetDefaultTemplate(template);
                    }
                }
                else
                {
                    // elements don't have a defaultTemplate, but it can be valid to have the old defaultTemplate in the db.
                    // (it would then re-appear if the user untoggles is element) See issue #203
                    //
                    // So we only log this as a problem if the default template is missing on a non-element doctype. 
                    if (!item.IsElement)
                    {

                        changes.AddUpdate("DefaultTemplate", item.DefaultTemplate?.Alias ?? string.Empty, "Cannot find Template", "DefaultTemplate", false);
                    }
                }
            }

            return changes;
        }

        private IEnumerable<uSyncChange> DeserializeTemplates(IContentType item, XElement node)
        {
            var templates = node?.Element(uSyncConstants.Xml.Info)?.Element("AllowedTemplates");
            if (templates == null) return Enumerable.Empty<uSyncChange>();

            var allowedTemplates = new List<ITemplate>();
            var changes = new List<uSyncChange>();


            foreach (var template in templates.Elements("Template"))
            {
                var alias = template.Value;
                var key = template.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);

                var templateItem = default(ITemplate);

                if (key != Guid.Empty)
                    templateItem = _fileService.GetTemplate(key);

                if (templateItem == null)
                    templateItem = _fileService.GetTemplate(alias);

                if (templateItem != null)
                {
                    logger.LogDebug("Adding Template: {alias}", templateItem.Alias);
                    allowedTemplates.Add(templateItem);
                }
            }


            var currentTemplates = string.Join(",", item.AllowedTemplates.Select(x => x.Alias).OrderBy(x => x));
            var newTemplates = string.Join(",", allowedTemplates.Select(x => x.Alias).OrderBy(x => x));

            if (currentTemplates != newTemplates)
            {
                changes.AddUpdate("AllowedTemplates", currentTemplates, newTemplates, "AllowedTemplates");
                item.AllowedTemplates = allowedTemplates;
            }

            return changes;
        }


        protected override Attempt<IContentType> CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var safeAlias = GetSafeItemAlias(alias);

            var item = new ContentType(shortStringHelper, -1)
            {
                Alias = alias
            };

            if (parent != null)
            {
                if (parent is IContentType parentContent)
                {
                    item.AddContentType(parentContent);
                }

                item.SetParent(parent);
            }

            // adds this alias to the alias cache. 
            AddAlias(safeAlias);

            return Attempt.Succeed((IContentType)item);
        }

        protected override void SaveContainer(EntityContainer container)
        {
            logger.LogDebug("Saving Container (In main class) {key}", container.Key.ToString());
            _contentTypeService.SaveContainer(container);
        }

        /// History Cleanup (added in v9.1) 


        private readonly string _historyCleanupName = "HistoryCleanup";
        private readonly string[] _historyCleanupProperties = new string[]
        {
            "PreventCleanup", "KeepAllVersionsNewerThanDays", "KeepLatestVersionPerDayForDays"
        };

        private XElement SerializeCleanupHistory(IContentType item)
        {
            if (!_capabilities.HasHistoryCleanup) return null;

            try
            {
                var historyCleanupInfo = item.GetType().GetProperty(_historyCleanupName);
                if (historyCleanupInfo == null) return null;

                var historyCleanup = historyCleanupInfo.GetValue(item);
                if (historyCleanup == null) return null;

                var history = new XElement(_historyCleanupName);
                foreach (var propertyName in _historyCleanupProperties)
                {
                    var property = historyCleanup.GetType().GetProperty(propertyName);
                    if (property != null)
                    {
                        history.Add(new XElement(property.Name, GetPropertyAs<string>(property, historyCleanup) ?? ""));
                    }
                }

                return history;

            }
            catch (Exception ex)
            {
                // we are very defensive. with the 'new' - if for some reason we can't read this, log it, but carry on.
                logger.LogWarning(ex, "Error trying to get the HistoryCleanup settings for this node.");
                return null;
            }
        }


        private IEnumerable<uSyncChange> DeserializeCleanupHistory(IContentType item, XElement node)
        {
            var emtpy = Enumerable.Empty<uSyncChange>();
            if (!_capabilities.HasHistoryCleanup || node == null) return emtpy;

            var cleanupNode = node.Element("Info")?.Element(_historyCleanupName);
            if (cleanupNode == null) return emtpy;

            try
            {
                // get the history cleanup property
                var historyCleanupInfo = item.GetType().GetProperty(_historyCleanupName);
                if (historyCleanupInfo == null) return emtpy;

                // get the history cleanup value
                var historyCleanup = historyCleanupInfo.GetValue(item);
                if (historyCleanup == null) return emtpy;

                var changes = new List<uSyncChange>();

                // go through the values in the XML 
                foreach (var element in cleanupNode.Elements())
                {
                    var property = historyCleanup.GetType().GetProperty(element.Name.LocalName);
                    if (property == null) continue;

                    var current = GetPropertyAs<string>(property, historyCleanup);
                    if (element.Value != current)
                    {
                        // now set it. 
                        var updatedValue = element.Value.TryConvertTo(property.PropertyType);
                        if (updatedValue.Success)
                        {
                            logger.LogDebug("Saving HistoryCleanup Value: {name} {value}", element.Name.LocalName, updatedValue.Result);
                            changes.AddUpdate($"{_historyCleanupName}:{element.Name.LocalName}", current ?? "(Blank)", updatedValue.Result, $"{_historyCleanupName}/{element.Name.LocalName}");
                            property.SetValue(historyCleanup, updatedValue.Result);
                        }
                    }
                }

                return changes;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error trying to get the HistoryCleanup settings for this node.");
                return emtpy;
            }

        }

        protected override XElement CleanseNode(XElement node)
        {
            // remove the history node when comparing, if this version doesn't support it but it is in the XML
            if (!_capabilities.HasHistoryCleanup && node.Element(uSyncConstants.Xml.Info)?.Element(_historyCleanupName) != null)
            {
                node.Element(uSyncConstants.Xml.Info).Element(_historyCleanupName).Remove();
            }

            return base.CleanseNode(node);
        }


        protected TValue GetPropertyAs<TValue>(PropertyInfo info, object property)
        {
            if (info == null) return default;

            var value = info.GetValue(property);
            if (value == null) return default;

            var result = value.TryConvertTo<TValue>();
            if (result.Success)
                return result.Result;

            return default;

        }
    }
}
