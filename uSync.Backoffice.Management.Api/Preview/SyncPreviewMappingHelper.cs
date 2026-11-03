using System.Xml.Linq;

using Umbraco.Cms.Api.Management.ViewModels.DataType;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice.Models;
using uSync.Core;
using uSync.Core.Extensions;
using uSync.Core.Serialization;

namespace uSync.Backoffice.Management.Api.Preview;

/// <inheritdoc/>
internal class SyncPreviewMappingHelper : ISyncPreviewMappingHelper
{
    private readonly IUmbracoMapper _umbracoMapper;
    private readonly IDataTypeService _dataTypeService;
    private readonly IEntityService _entityService;
    private readonly ITemplateService _templateService;

    public SyncPreviewMappingHelper(
        IUmbracoMapper umbracoMapper,
        IDataTypeService dataTypeService,
        IEntityService entityService,
        ITemplateService templateService)
    {
        _umbracoMapper = umbracoMapper;
        _dataTypeService = dataTypeService;
        _entityService = entityService;
        _templateService = templateService;
    }

    /// <inheritdoc/>
    public TResponse? Map<TItem, TResponse>(object? item)
        where TResponse : class
        => item is TItem typed ? _umbracoMapper.Map<TResponse>(typed) : null;

    /// <inheritdoc/>
    public List<TResponse> MapCompositions<TItem, TResponse>(IEnumerable<IContentTypeComposition> types)
        where TItem : IContentTypeComposition
        where TResponse : class
    {
        var found = new Dictionary<Guid, TItem>();
        var queue = new Queue<IContentTypeComposition>(
            types.SelectMany(x => x.ContentTypeComposition));

        while (queue.TryDequeue(out var composition))
        {
            if (composition is not TItem typed || found.ContainsKey(typed.Key)) continue;
            found[typed.Key] = typed;

            foreach (var child in typed.ContentTypeComposition)
                queue.Enqueue(child);
        }

        return found.Values
            .Select(x => _umbracoMapper.Map<TResponse>(x))
            .OfType<TResponse>()
            .ToList();
    }

    /// <inheritdoc/>
    public List<Guid> GetNewCompositions(SyncItemPreview preview)
        => [.. preview.PendingItems
            .OfType<IContentTypeComposition>()
            .Select(x => x.Key)];

    /// <inheritdoc/>
    /// <remarks>
    ///  data types that aren't in the site yet are built from the uSync files,
    ///  so new properties can still show their editor.
    /// </remarks>
    public async Task<(List<DataTypeResponseModel> DataTypes, List<Guid> NewDataTypes)> MapDataTypesAsync(
        IEnumerable<IContentTypeComposition> types, SyncItemPreview preview)
    {
        var keys = types
            .SelectMany(x => x.CompositionPropertyTypes)
            .Select(x => x.DataTypeKey)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (keys.Length == 0) return ([], []);

        var dataTypes = (await _dataTypeService.GetAllAsync(keys)).ToList();

        // data types that aren't in the site yet were built from the uSync files during the preview.
        var pending = preview.PendingItems
            .OfType<IDataType>()
            .Where(x => keys.Contains(x.Key) && dataTypes.All(d => d.Key != x.Key))
            .ToList();

        dataTypes.AddRange(pending);

        return (
            dataTypes
                .Select(x => _umbracoMapper.Map<DataTypeResponseModel>(x))
                .OfType<DataTypeResponseModel>()
                .ToList(),
            [.. pending.Select(x => x.Key)]);
    }

    /// <inheritdoc/>
    public async Task<SyncContentPreviewInfo?> MapContentInfoAsync(IContentBase? item, UmbracoObjectTypes objectType, XElement? node, SyncSerializerOptions? options)
    {
        if (item is null) return null;

        // content sets the sort order and trashed state in the second pass (which a preview doesn't run),
        // so for the incoming item they are read from the file, following the same settings as the import.
        var nodeInfo = node?.Element(uSyncConstants.Xml.Info);
        var ignoreSortOrder = options?.GetSetting(uSyncConstants.DefaultSettings.IgnoreSortOrder, uSyncConstants.DefaultSettings.IgnoreSortOrder_Default) ?? false;

        var info = new SyncContentPreviewInfo
        {
            Parent = GetParentName(item, objectType, node),
            SortOrder = nodeInfo is null || ignoreSortOrder
                ? item.SortOrder
                : nodeInfo.Element(uSyncConstants.Xml.SortOrder).ValueOrDefault(item.SortOrder),
            Trashed = nodeInfo?.Element("Trashed").ValueOrDefault(item.Trashed) ?? item.Trashed,
        };

        if (item is IContent content)
        {
            if (content.TemplateId is int templateId)
                info.Template = (await _templateService.GetAsync(templateId))?.Alias;

            // only the cultures uSync is set to sync are compared, the import leaves the others alone.
            var cultures = options?.GetCultures() ?? [];
            info.Published = [.. (node is null ? GetPublishedState(content) : GetPublishedState(node))
                .Where(x => cultures.IsValidOrBlank(x.Culture))];
        }

        return info;
    }

    /// <summary>
    ///  the parent's name, from the site or (for a parent that isn't in the site yet) the uSync file.
    /// </summary>
    private string? GetParentName(IContentBase item, UmbracoObjectTypes objectType, XElement? node)
    {
        if (item.ParentId > 0)
            return _entityService.Get(item.ParentId, objectType)?.Name;

        var parentNode = node?.Element(uSyncConstants.Xml.Info)?.Element(uSyncConstants.Xml.Parent);
        if (parentNode is not null && parentNode.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty) != Guid.Empty)
            return parentNode.ValueOrDefault(string.Empty);

        return null;
    }

    private static List<SyncContentPreviewCulture> GetPublishedState(IContent content)
        => content.ContentType.VariesByCulture()
            ? [.. content.AvailableCultures.Order().Select(culture => new SyncContentPreviewCulture
            {
                Culture = culture,
                Published = content.IsCulturePublished(culture),
            })]
            : [new SyncContentPreviewCulture { Published = content.Published }];

    /// <summary>
    ///  the published state the uSync file will leave the item in.
    /// </summary>
    private static List<SyncContentPreviewCulture> GetPublishedState(XElement node)
    {
        var published = node.Element(uSyncConstants.Xml.Info)?.Element("Published");
        if (published is null) return [];

        if (published.HasElements)
        {
            return [.. published.Elements("Published")
                .Select(x => new SyncContentPreviewCulture
                {
                    Culture = x.Attribute("Culture").ValueOrDefault(string.Empty),
                    Published = x.ValueOrDefault(false),
                })
                .OrderBy(x => x.Culture)];
        }

        return [new SyncContentPreviewCulture { Published = published.Attribute("Default").ValueOrDefault(false) }];
    }
}
