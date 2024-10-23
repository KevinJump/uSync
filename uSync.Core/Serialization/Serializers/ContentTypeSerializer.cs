using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Org.BouncyCastle.Asn1.Cms;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.Core.Extensions;
using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("B3F7F247-6077-406D-8480-DB1004C8211C", "ContentTypeSerializer", uSyncConstants.Serialization.ContentType)]
public class ContentTypeSerializer : ContentTypeBaseSerializer<IContentType>, ISyncSerializer<IContentType>
{
    private readonly IContentTypeService _contentTypeService;
    private readonly ITemplateService _templateService;

    private readonly uSyncCapabilityChecker _capabilities;

    public ContentTypeSerializer(
        IEntityService entityService,
        ILogger<ContentTypeSerializer> logger,
        IDataTypeService dataTypeService,
        IContentTypeService contentTypeService,
        IContentTypeContainerService contentTypeContainerService,
        ITemplateService templateService,
        IShortStringHelper shortStringHelper,
        AppCaches appCaches,
        uSyncCapabilityChecker uSyncCapabilityChecker)
        : base(entityService, contentTypeContainerService, logger, dataTypeService, contentTypeService, UmbracoObjectTypes.DocumentTypeContainer, shortStringHelper, appCaches)
    {
        _contentTypeService = contentTypeService;
        _templateService = templateService;
        _capabilities = uSyncCapabilityChecker;
    }

    protected override void EnsureAliasCache()
    {
        aliasCache = _appCache.GetCacheItem<List<string>>(
            $"usync_{this.Id}", () =>
            {
                var sw = Stopwatch.StartNew();
                var aliases = _contentTypeService.GetAllContentTypeAliases().ToList();
                sw.Stop();
                this.logger.LogDebug("Cache hit, 'usync_{id}' fetching all aliases {time}ms", this.Id, sw.ElapsedMilliseconds);
                return aliases;
            });
    }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(IContentType item, SyncSerializerOptions options)
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
            var folderNode = await this.GetFolderNodeAsync(item);
            if (folderNode != null)
                info.Add(folderNode);
        }

        // compositions ? 
        info.Add(SerializeCompositions((ContentTypeCompositionBase)item));

        // templates
        var templateAlias =
            (item.DefaultTemplate != null && item.DefaultTemplate.Id != 0)
            ? item.DefaultTemplate.Alias
            : "";

        info.Add(new XElement("DefaultTemplate", templateAlias));

        var templates = SerializeTemplates(item);
        if (templates != null)
            info.Add(templates);

        node.Add(info);
        node.Add(SerializeStructure(item));
        node.Add(SerializePropertiesAsync(item));
        node.Add(SerializeTabs(item));

        return SyncAttempt<XElement>.Succeed(item.Name ?? item.Alias, node, typeof(IContentType), ChangeType.Export);
    }

    protected override void SerializeExtraProperties(XElement node, IContentType item, IPropertyType property)
    {
        node.Add(new XElement("Variations", property.Variations));
    }

    private static XElement SerializeTemplates(IContentType item)
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

    protected override async Task<SyncAttempt<IContentType>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
    { 
        var attempt = await FindOrCreateAsync(node);
        if (attempt.Success == false || attempt.Result is null)
            throw attempt.Exception ?? new Exception($"Unknown error {node.GetAlias()}");

        var item = attempt.Result;

        var details = new List<uSyncChange>();

        details.AddRange(await DeserializeBaseAsync(item, node));


        // compositions
        details.AddRange(await DeserializeCompositionsAsync(item, node));

        // tabs
        details.AddRange(DeserializeTabs(item, node));

        // properties
        details.AddRange(await DeserializePropertiesAsync(item, node, options));

        // content type only property stuff.
        details.AddRange(await DeserializeContentTypePropertiesAsync(item, node));

        // templates 
        details.AddRange(await DeserializeTemplatesAsync(item, node, options));

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

        return [];
    }

    public override async Task<SyncAttempt<IContentType>> DeserializeSecondPassAsync(IContentType item, XElement node, SyncSerializerOptions options)
    {
        logger.LogDebug("Deserialize Second Pass {alias}", item.Alias);

        var details = new List<uSyncChange>();

        SetSafeAliasValue(item, node, false);

        // we can do this here, hopefully its not needed 
        // as we graph sort at the start,
        // so it should say 'no changes' on a second pass.
        details.AddRange(await DeserializeCompositionsAsync(item, node));

        details.AddRange(await DeserializeStructureAsync(item, node));

        // When doing this reflection-y - it doesn't set is dirty. 
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

            await _contentTypeService.UpdateAsync(item, Constants.Security.SuperUserKey);
        }

        await CleanFolderAsync(item, node);

        return SyncAttempt<IContentType>.Succeed(item.Name ?? item.Alias, item, ChangeType.Import, "", saveInSerializer, details);
    }

    private async Task<List<uSyncChange>> DeserializeContentTypePropertiesAsync(IContentType item, XElement node)
    {
        var info = node?.Element("Info");
        if (info is null) return [];

        var changes = new List<uSyncChange>();

        var listView = info.Element("ListView").ValueOrDefault(Guid.Empty);
        if (listView != Guid.Empty && item.ListView != listView)
        {
            changes.AddUpdate("ListView", item.ListView, listView, "Info/ListView");
            item.ListView = listView;
        }


        var masterTemplate = info?.Element("DefaultTemplate").ValueOrDefault(string.Empty) ?? string.Empty;
        if (!string.IsNullOrEmpty(masterTemplate))
        {
            var template = await _templateService.GetAsync(masterTemplate);
            if (template is not null)
            {
                if (item.DefaultTemplate is null || template.Alias != item.DefaultTemplate.Alias)
                {
                    changes.AddUpdate("DefaultTemplate", item.DefaultTemplate?.Alias ?? string.Empty, masterTemplate, "DefaultTemplate");
                    item.SetDefaultTemplate(template);
                }
            }
            else
            {
                // elements don't have a defaultTemplate, but it can be valid to have the old defaultTemplate in the db.
                // (it would then re-appear if the user un-toggles is element) See issue #203
                //
                // So we only log this as a problem if the default template is missing on a non-element doctype. 
                if (item.IsElement is false)
                {

                    changes.AddUpdate("DefaultTemplate", item.DefaultTemplate?.Alias ?? string.Empty, "Cannot find Template", "DefaultTemplate", false);
                }
            }
        }

        return changes;
    }

    private async Task<List<uSyncChange>> DeserializeTemplatesAsync(IContentType item, XElement node, SyncSerializerOptions options)
    {
        var templates = node?.Element(uSyncConstants.Xml.Info)?.Element("AllowedTemplates");
        if (templates is null) return [];

        var allowedTemplates = new List<ITemplate>();
        var changes = new List<uSyncChange>();


        foreach (var template in templates.Elements("Template"))
        {
            var alias = template.Value;
            var key = template.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty);

            var templateItem = default(ITemplate);

            if (key != Guid.Empty)
                templateItem = await _templateService.GetAsync(key);

            templateItem ??= await _templateService.GetAsync(alias);

            if (templateItem is not null)
            {
                logger.LogDebug("Adding Template: {alias}", templateItem.Alias);
                allowedTemplates.Add(templateItem);
            }
        }

        var currentTemplates = string.Join(",", item.AllowedTemplates?.Select(x => x.Alias).OrderBy(x => x) ?? Enumerable.Empty<string>());
        var newTemplates = string.Join(",", allowedTemplates.Select(x => x.Alias).OrderBy(x => x));

        // New "KeepTemplates" Option, will merge uSync templates with existing ones (not a complete sync!)
        if (options.GetSetting<bool>("KeepTemplates", false))
        {
            allowedTemplates =
            [
                ..allowedTemplates,
                ..item.AllowedTemplates?.Where(x => !newTemplates.InvariantContains(x.Alias)) ?? [],
            ];
            newTemplates = string.Join(",", allowedTemplates.Select(x => x.Alias).OrderBy(x => x));
        }

        if (currentTemplates != newTemplates)
        {
            changes.AddUpdate("AllowedTemplates", currentTemplates, newTemplates, "AllowedTemplates");
            item.AllowedTemplates = allowedTemplates;
        }

        return changes;
    }

    protected override Task<Attempt<IContentType?>> CreateItemAsync(string alias, ITreeEntity? parent, string itemType)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            var safeAlias = GetSafeItemAlias(alias);

            var item = new ContentType(shortStringHelper, -1)
            {
                Alias = alias
            };

            if (parent is not null)
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
        });
    }

    /// History Cleanup (added in v9.1) 

    private readonly string _historyCleanupName = "HistoryCleanup";
    private readonly string[] _historyCleanupProperties = [
        "PreventCleanup",
        "KeepAllVersionsNewerThanDays",
        "KeepLatestVersionPerDayForDays"
    ];

    private XElement? SerializeCleanupHistory(IContentType item)
    {
        if (!_capabilities.HasHistoryCleanup) return null;

        try
        {
            var historyCleanupInfo = item.GetType().GetProperty(_historyCleanupName);
            if (historyCleanupInfo is null) return null;

            var historyCleanup = historyCleanupInfo.GetValue(item);
            if (historyCleanup is null) return null;

            var history = new XElement(_historyCleanupName);
            foreach (var propertyName in _historyCleanupProperties)
            {
                var property = historyCleanup.GetType().GetProperty(propertyName);
                if (property is not null)
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


    private List<uSyncChange> DeserializeCleanupHistory(IContentType item, XElement node)
    {
        if (!_capabilities.HasHistoryCleanup || node == null) return [];

        var cleanupNode = node.Element("Info")?.Element(_historyCleanupName);
        if (cleanupNode is null) return [];

        try
        {
            // get the history cleanup property
            var historyCleanupInfo = item.GetType().GetProperty(_historyCleanupName);
            if (historyCleanupInfo is null) return [];

            // get the history cleanup value
            var historyCleanup = historyCleanupInfo.GetValue(item);
            if (historyCleanup is null) return [];

            var changes = new List<uSyncChange>();

            // go through the values in the XML 
            foreach (var element in cleanupNode.Elements())
            {
                var property = historyCleanup.GetType().GetProperty(element.Name.LocalName);
                if (property is null) continue;

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
            return [];
        }

    }

    protected override XElement CleanseNode(XElement node)
    {
        // remove the history node when comparing, if this version doesn't support it but it is in the XML
        if (!_capabilities.HasHistoryCleanup && node.Element(uSyncConstants.Xml.Info)?.Element(_historyCleanupName) != null)
        {
            node.Element(uSyncConstants.Xml.Info)?.Element(_historyCleanupName)?.Remove();
        }

        return base.CleanseNode(node);
    }


    protected static TValue? GetPropertyAs<TValue>(PropertyInfo info, object property)
    {
        if (info is null) return default;

        var value = info.GetValue(property);
        if (value is null) return default;

        var result = value.TryConvertTo<TValue>();
        if (result.Success)
            return result.Result;

        return default;

    }
}
