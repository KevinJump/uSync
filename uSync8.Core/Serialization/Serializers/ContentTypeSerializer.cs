using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

using uSync8.Core.Extensions;
using uSync8.Core.Models;

namespace uSync8.Core.Serialization.Serializers
{
    [SyncSerializer("B3F7F247-6077-406D-8480-DB1004C8211C", "ContentTypeSerializer", uSyncConstants.Serialization.ContentType)]
    public class ContentTypeSerializer : ContentTypeBaseSerializer<IContentType>, ISyncNodeSerializer<IContentType>
    {
        private readonly IContentTypeService contentTypeService;
        private readonly IFileService fileService;

        public ContentTypeSerializer(
            IEntityService entityService, ILogger logger,
            IDataTypeService dataTypeService,
            IContentTypeService contentTypeService,
            IFileService fileService)
            : base(entityService, logger, dataTypeService, contentTypeService, UmbracoObjectTypes.DocumentTypeContainer)
        {
            this.contentTypeService = contentTypeService;
            this.fileService = fileService;
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
                info.Add(new XElement("Parent", parent.Alias,
                            new XAttribute("Key", parent.Key)));
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

        protected override void SerializeExtraProperties(XElement node, IContentType item, PropertyType property)
        {
            node.Add(new XElement("Variations", property.Variations));
        }

        private XElement SerailizeTemplates(IContentType item)
        {
            var node = new XElement("AllowedTemplates");
            if (item.AllowedTemplates.Any())
            {
                foreach (var template in item.AllowedTemplates.OrderBy(x => x.Alias))
                {
                    node.Add(new XElement("Template", template.Alias,
                        new XAttribute("Key", template.Key)));
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

            // contentTypeService.Save(item);

            return SyncAttempt<IContentType>.Succeed(item.Name, item, ChangeType.Import, details);
        }

        protected override IEnumerable<uSyncChange> DeserializeExtraProperties(IContentType item, PropertyType property, XElement node)
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
            var details = new List<uSyncChange>();

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
                logger.Debug<ContentTypeSerializer>("Saving in Serializer because item is dirty [{properties}]", dirty); 

                contentTypeService.Save(item);
            }

            CleanFolder(item, node);

            return SyncAttempt<IContentType>.Succeed(item.Name, item, ChangeType.Import, saveInSerializer, details);
              
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
                var template = fileService.GetTemplate(masterTemplate);
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
            var templates = node?.Element("Info")?.Element("AllowedTemplates");
            if (templates == null) return Enumerable.Empty<uSyncChange>();

            var allowedTemplates = new List<ITemplate>();
            var changes = new List<uSyncChange>();


            foreach (var template in templates.Elements("Template"))
            {
                var alias = template.Value;
                var key = template.Attribute("Key").ValueOrDefault(Guid.Empty);

                var templateItem = default(ITemplate);

                if (key != Guid.Empty)
                    templateItem = fileService.GetTemplate(key);

                if (templateItem == null)
                    templateItem = fileService.GetTemplate(alias);

                if (templateItem != null)
                {
                    logger.Debug<ContentTypeSerializer>("Adding Template: {0}", templateItem.Alias);
                    allowedTemplates.Add(templateItem);
                }
            }


            var currentTemplates = string.Join(",", item.AllowedTemplates.Select(x => x.Alias).OrderBy(x => x));
            var newTemplates = string.Join(",", allowedTemplates.Select(x => x.Alias).OrderBy(x => x));

            if (currentTemplates != newTemplates)
            {
                changes.AddUpdate("AllowedTemplates", currentTemplates, newTemplates, "AllowedTemplates");
            }

            item.AllowedTemplates = allowedTemplates;

            return changes;
        }


        protected override Attempt<IContentType> CreateItem(string alias, ITreeEntity parent, string itemType)
        {
            var item = new ContentType(-1)
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

            return Attempt.Succeed((IContentType)item);
        }

        protected override void SaveContainer(EntityContainer container)
        {
            logger.Debug<ContentTypeSerializer>("Saving Container (In main class) {0}", container.Key.ToString());
            contentTypeService.SaveContainer(container);
        }

        private bool HasHistoryCleanup()
            => UmbracoVersion.LocalVersion.Major >= 8 && UmbracoVersion.LocalVersion.Minor >= 18;

        private string _historyCleanupName = "HistoryCleanup";
        private string[] _historyCleanupProperties = new string[]
        {
            "PreventCleanup", "KeepAllVersionsNewerThanDays", "KeepLatestVersionPerDayForDays"
        };

        /// History Cleanup (added in v8.18) 
        /// 
        private XElement SerializeCleanupHistory(IContentType item)
        {
            if (HasHistoryCleanup())
            {
                try
                {
                    var historyCleanupInfo = item.GetType().GetProperty(_historyCleanupName);
                    if (historyCleanupInfo != null)
                    {
                        var historyCleanup = historyCleanupInfo.GetValue(item);
                        if (historyCleanup != null)
                        {
                            var history = new XElement(_historyCleanupName);
                            foreach (var propertyName in _historyCleanupProperties) {
                                var property = historyCleanup.GetType().GetProperty(propertyName);
                                if (property != null)
                                {
                                    history.Add(new XElement(property.Name, GetPropertyAs<string>(property, historyCleanup) ?? ""));
                                }
                            }
                            return history;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // we are very defensive. with the 'new' - if for some reason we can't read this, log it, but carry on.
                    logger.Warn<ContentTypeSerializer>(ex, "Error tryng to get the HistoryCleanup settings for this node.");
                }
            }

            return null;
        }


        private IEnumerable<uSyncChange> DeserializeCleanupHistory(IContentType item, XElement node)
        {
            var emtpy = Enumerable.Empty<uSyncChange>();
            if (!HasHistoryCleanup() || node == null) return emtpy;

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
                            logger.Debug<ContentTypeSerializer>("Saving HistoryCleanup Value: {name} {value}", element.Name.LocalName, updatedValue.Result);
                            changes.AddUpdate($"{_historyCleanupName}:{element.Name.LocalName}", current, updatedValue.Result, $"{_historyCleanupName}/{element.Name.LocalName}");
                            property.SetValue(historyCleanup, updatedValue.Result);
                        }
                    }
                }

                return changes;
            }
            catch(Exception ex)
            {
                logger.Warn<ContentTypeSerializer>(ex, "Error tryng to get the HistoryCleanup settings for this node.");
            }

            return emtpy;
        }

        protected override XElement CleanseNode(XElement node)
        {
            if (!HasHistoryCleanup() && node.Element("Info")?.Element(_historyCleanupName) != null)
            {
                node.Element("Info").Element(_historyCleanupName).Remove();
            }
                
            return base.CleanseNode(node);
        }
    }
}
