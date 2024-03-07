using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Models;

namespace uSync.Core.Serialization.Serializers;

[SyncSerializer("9A5C253C-71FA-4FC0-9B7C-9D0522AAE880", "Domain Serializer", uSyncConstants.Serialization.Domain)]
public class DomainSerializer : SyncSerializerBase<IDomain>, ISyncSerializer<IDomain>
{
    private readonly IDomainService _domainService;
    private readonly IContentService _contentService;
    private readonly ILocalizationService _localizationService;
    private readonly uSyncCapabilityChecker _capabilityChecker;

    public DomainSerializer(IEntityService entityService, ILogger<DomainSerializer> logger,
        IDomainService domainService,
        IContentService contentService,
        ILocalizationService localizationService,
        uSyncCapabilityChecker capabilityChecker)
        : base(entityService, logger)
    {
        this._domainService = domainService;
        this._contentService = contentService;
        this._localizationService = localizationService;
        _capabilityChecker = capabilityChecker;
    }

    protected override SyncAttempt<IDomain> DeserializeCore(XElement node, SyncSerializerOptions options)
    {
        var item = FindOrCreate(node);

        var info = node.Element(uSyncConstants.Xml.Info);
        if (info is null)
        {
            return SyncAttempt<IDomain>.Fail(node.GetAlias(), default, ChangeType.Fail, "Missing info section in xml", new KeyNotFoundException("Missing Info Section in XML"));
        }

        var isoCode = info.Element("Language").ValueOrDefault(string.Empty) ?? string.Empty;

        var changes = new List<uSyncChange>();

        if (!string.IsNullOrWhiteSpace(isoCode))
        {
            var language = _localizationService.GetLanguageByIsoCode(isoCode);
            if (language != null && item.LanguageId != language.Id)
            {
                changes.AddUpdate("Id", item.LanguageId, language.Id);
                item.LanguageId = language.Id;
            }
        }

        var rootItem = default(IContent);

        var rootKey = info.Element("Root")?.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty) ?? Guid.Empty;
        if (rootKey != Guid.Empty)
        {
            rootItem = _contentService.GetById(rootKey);
        }

        if (rootItem == default(IContent))
        {
            var rootName = info.Element("Root").ValueOrDefault(string.Empty);
            if (rootName != string.Empty)
            {
                rootItem = FindByPath(rootName.ToDelimitedList("/"));
            }
        }

        if (rootItem != default(IContent) && item.RootContentId != rootItem.Id)
        {
            changes.AddUpdate("RootItem", item.RootContentId, rootItem.Id);
            item.RootContentId = rootItem.Id;
        }

        if (_capabilityChecker?.HasSortableDomains == true)
        {
            var sortOrder = info.Element(_sortablePropertyName).ValueOrDefault(-2);
            var existing = GetSortableValue(item);
            if (sortOrder != -2 && sortOrder != existing)
            {
                changes.AddUpdate(_sortablePropertyName, sortOrder, existing);
                SetSortableValue(item, sortOrder);
            }
        }

        return SyncAttempt<IDomain>.Succeed(item.DomainName, item, ChangeType.Import, changes);

    }

    private IDomain FindOrCreate(XElement node)
    {
        var item = FindItem(node);
        if (item != null) return item;

        return new UmbracoDomain(node.GetAlias());

    }

    protected override SyncAttempt<XElement> SerializeCore(IDomain item, SyncSerializerOptions options)
    {
        var node = new XElement(ItemType,
            new XAttribute(uSyncConstants.Xml.Key, item.DomainName.GetDeterministicHashCode().ToGuid()),
            new XAttribute(uSyncConstants.Xml.Alias, item.DomainName));

        var info = new XElement(uSyncConstants.Xml.Info,
            new XElement("IsWildcard", item.IsWildcard),
            new XElement("Language", item.LanguageIsoCode));


        if (item.RootContentId.HasValue)
        {
            var rootNode = _contentService.GetById(item.RootContentId.Value);

            if (rootNode != null)
            {
                info.Add(new XElement("Root", GetItemPath(rootNode),
                    new XAttribute(uSyncConstants.Xml.Key, rootNode.Key)));
            }
        }

        if (_capabilityChecker?.HasSortableDomains == true)
        {
            // domains can be sorted. we need to get the value via reflection.
            info.Add(new XElement(_sortablePropertyName, GetSortableValue(item)));
        }

        node.Add(info);

        return SyncAttempt<XElement>.SucceedIf(
            node != null, item.DomainName, node, typeof(IDomain), ChangeType.Export);
    }

    private const string _sortablePropertyName = "SortOrder";

    /// <summary>
    ///  Retrieve the SortOrder value for the item
    /// </summary>
    /// <remarks>
    ///  11.3.0+ sortable value got added to the IDomain interface.
    /// </remarks>
    private static int GetSortableValue(IDomain item)
    {
        var property = item.GetType().GetProperty(_sortablePropertyName);
        if (property == null) return 0;

        var result = property.GetValue(item);

        var attempt = result.TryConvertTo<int>();
        return attempt.Success ? attempt.Result : 0;
    }

    /// <summary>
    ///  Set the SortOrder value for the item
    /// </summary>
    /// <remarks>
    ///  11.3.0+ sortable value got added to the IDomain interface.
    /// </remarks>
    private static void SetSortableValue(IDomain item, int sortOrder)
    {
        var property = item.GetType().GetProperty(_sortablePropertyName);
        if (property == null) return;

        property.SetValue(item, sortOrder);
    }

    /// <summary>
    ///  clean the xml of properties we don't want to compare
    /// </summary>
    /// <remarks>
    ///  in v11.3.0+ sort order got added, if we are running less than this, we want to 
    ///  remove the sort order property from the xml so it doesn't get compared when it 
    ///  can't be set. 
    /// </remarks>
    protected override XElement CleanseNode(XElement node)
    {
        if (_capabilityChecker?.HasSortableDomains != true
            && node.Element(uSyncConstants.Xml.Info)?.Element(_sortablePropertyName) != null)
        {
            // doesn't have sortable domains, remove the sortable value from the XML
            // so we don't get a false positive when down syncing.
            node.Element(uSyncConstants.Xml.Info)?.Element(_sortablePropertyName)?.Remove();
        }

        return base.CleanseNode(node);
    }

    public override IDomain? FindItem(int id)
        => _domainService.GetById(id);

    public override IDomain? FindItem(Guid key)
        => _domainService.GetAll(true).FirstOrDefault(x => x.Key == key);

    public override IDomain? FindItem(string alias)
        => _domainService.GetByName(alias);


    /// <summary>
    ///  these items do exist in the content serializer, 
    ///  but they are here because we are not doing this
    ///  for our primary object type but for content.
    /// </summary>

    protected virtual string GetItemPath(IContent? item)
    {
        if (item is null) return string.Empty;

        var entity = entityService.Get(item.Id);
        return GetItemPath(entity) ?? string.Empty;
    }

    private string GetItemPath(IEntitySlim? item)
    {
        if (item is null) return string.Empty;

        var path = "";
        if (item.ParentId != -1)
        {
            var parent = entityService.Get(item.ParentId);
            if (parent != null)
                path += GetItemPath(parent);
        }

        return path += "/" + item.Name;
    }

    private IContent? FindByPath(IEnumerable<string> folders)
    {
        var item = default(IContent);
        foreach (var folder in folders)
        {
            var next = FindContentItem(folder, item);
            if (next == null)
                return item;

            item = next;
        }

        return item;
    }

    private IContent? FindContentItem(string alias, IContent? parent)
    {
        if (parent is null) return null;

        var children = entityService.GetChildren(parent.Id, UmbracoObjectTypes.Document);
        var child = children.FirstOrDefault(x => x.Name.InvariantEquals(alias));
        if (child != null)
            return _contentService.GetById(child.Id);

        return null;
    }


    public override void SaveItem(IDomain item)
        => _domainService.Save(item);

    public override void DeleteItem(IDomain item)
        => _domainService.Delete(item);

    public override string ItemAlias(IDomain item)
        => item.DomainName;
}
