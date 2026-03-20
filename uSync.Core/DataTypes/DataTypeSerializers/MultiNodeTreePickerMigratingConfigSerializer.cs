using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using uSync.Core.Extensions;

namespace uSync.Core.DataTypes.DataTypeSerializers;

internal class MultiNodeTreePickerMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IMemberTypeService _memberTypeService;

    public MultiNodeTreePickerMigratingConfigSerializer(
        IContentTypeService contentTypeService,
        IMediaTypeService mediaTypeService,
        IMemberTypeService memberTypeService)
    {
        _contentTypeService = contentTypeService;
        _mediaTypeService = mediaTypeService;
        _memberTypeService = memberTypeService;
    }

    public string Name => nameof(MultiNodeTreePickerMigratingConfigSerializer);

    public string[] Editors => [
        Constants.PropertyEditors.Aliases.MultiNodeTreePicker
    ];

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue("filter", out var filterValue) is false || filterValue is null)
            return configuration;

        var filter = filterValue.ToString();
        if (string.IsNullOrWhiteSpace(filter) || filter.Equals("null", StringComparison.OrdinalIgnoreCase))
            return configuration;

        // we need a type type to continue.
        var filterType = GetTreeType(configuration);
        if (filterType is null)
            return configuration;

        var filters = filter.Split(",");
        List<Guid> keys = new(filters.Length);

        foreach (var alias in filters)
        {
            if (Guid.TryParse(alias, out var _) is true)
                continue;

            var key = GetKeyFromTypeAlias(filterType, alias);
            if (key.HasValue)
                keys.Add(key.Value);
        }

        if (keys.Count > 0)
            configuration["filter"] = string.Join(",", keys);

        return configuration;
    }

    private static string? GetTreeType(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue("treeSource", out var treeSourceValue) is false || treeSourceValue is null)
            return "content";

        if (treeSourceValue.TryConvertToJsonObject(out var treeSource) is false)
            return "content";

        if (treeSource.TryGetPropertyValue("type", out var typeValue) is false)
            return "content";

        return typeValue?.GetValueAs<string>();
    }

    private Guid? GetKeyFromTypeAlias(string type, string alias)
        => type switch
        {
            "content" => _contentTypeService.Get(alias)?.Key,
            "media" => _mediaTypeService.Get(alias)?.Key,
            "member" => _memberTypeService.Get(alias)?.Key,
            _ => null,
        };

}

