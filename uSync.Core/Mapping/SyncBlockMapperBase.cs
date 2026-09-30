using Jumoo.Json;

using Microsoft.Extensions.Logging;

using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Mapping.Mappers;
using uSync.Core.Serialization;

namespace uSync.Core.Mapping;

public abstract class SyncBlockMapperBase<TBlockValue> : SyncValueMapperBase
    where TBlockValue : BlockValue
{
    private readonly IContentTypeService _contentTypeService;
    private readonly Lazy<SyncValueMapperCollection> _mapperCollection;
    private readonly ILogger<SyncBlockMapperBase<TBlockValue>> _logger;

    public SyncBlockMapperBase(
        IEntityService entityService,
        IContentTypeService contentTypeService,
        Lazy<SyncValueMapperCollection> mapperCollection,
        ILogger<SyncBlockMapperBase<TBlockValue>> logger)
        : base(entityService)
    {
        _contentTypeService = contentTypeService;
        _mapperCollection = mapperCollection;
        _logger = logger;
    }

    /// <summary>
    ///  serializer options for imported block values, these keep the property order Umbraco uses. 
    /// </summary>
    /// <remarks>
    ///  when an invariant block editor contains culture variant elements, Umbraco re-serializes 
    ///  the value on publish (merging each culture) and then does a string compare against the 
    ///  draft to work out if the item has pending changes. so the draft we save has to be exactly 
    ///  what Umbraco would write (#1097), our default options sort properties alphabetically.
    /// </remarks>
    private static readonly JsonSerializerOptions _importOptions = new(JsonTextOptions.GetOptions(false))
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public override async Task<string?> GetImportValueAsync(string value, string editorAlias, SyncSerializerOptions options)
        => await ProcessBlockValuesAsync(value, GetImportProperty, options, isImport: true);

    public override async Task<string?> GetExportValueAsync(object value, string editorAlias)
        => await ProcessBlockValuesAsync(value?.ToString() ?? string.Empty, GetExportProperty, new(), isImport: false);

    private static string? GetStringValue(object? value)
    {
        if (value is null) return string.Empty;

        return value switch
        {
            string stringValue => stringValue,
            IList or JsonArray or JsonObject => value.SerializeJsonString(false),
            _ => value.ToString(),
        };
    }

    private async Task<object?> GetImportProperty(object? value, IPropertyType? propertyType, SyncSerializerOptions options)
    {
        if (_mapperCollection.Value is null || propertyType is null) return value;

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Importing block value for {PropertyEditorAlias} {valueType}", propertyType.PropertyEditorAlias, value?.GetType().Name ?? "blank");

        var importString = SyncBlockMapperBase<TBlockValue>.GetStringValue(value) ?? string.Empty;

        // revert this back to the old way - we don't expand the json we get back because umbraco is very 
        // sensitve to what the exact format of the blocks is, and if we expand them, then calls during render
        // can return null. 
        return await _mapperCollection.Value.GetImportValueAsync(importString, propertyType, options);
    }

    private async Task<object?> GetExportProperty(object? value, IPropertyType? propertyType, SyncSerializerOptions options)
    {
        if (_mapperCollection.Value is null || propertyType is null) 
            return value;        

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Exporting block value for {PropertyEditorAlias} {valueType}", propertyType.PropertyEditorAlias, value?.GetType().Name ?? "blank");

        var exportValueAsString = SyncBlockMapperBase<TBlockValue>.GetStringValue(value) ?? string.Empty;
        var result = await _mapperCollection.Value.GetExportValueAsync(exportValueAsString, propertyType.PropertyEditorAlias);
        return result.ConvertToJsonNode()?.ExpandAllJsonInToken() ?? result;
    }

    private async Task<string?> ProcessBlockValuesAsync(string value, Func<object?, IPropertyType?, SyncSerializerOptions, Task<object?>> GetValueMethod, SyncSerializerOptions options, bool isImport)
    {
        var blockValue = SyncBlockMapperBase<TBlockValue>.GetBlockValue(value);
        if (blockValue == null) return value;

        List<BlockItemData> blocks = [
            ..blockValue.ContentData,
            ..blockValue.SettingsData
        ];

        foreach (var contentItem in blocks)
        {
            MigrateBlock(contentItem);

            await ProcessBlockData(contentItem, GetValueMethod, options, isImport);
        }

        if (blockValue.Expose.Count == 0)
        {
            // migration from v14 to v15+ block values.
            blockValue.Expose = [.. blockValue.ContentData.Select(x => new BlockItemVariation(x.Key, null, null))];
        }

        if (isImport)
            return JsonSerializer.Serialize(blockValue, _importOptions);

        return blockValue.SerializeJsonString(true);
    }

    private async Task ProcessBlockData(BlockItemData? blockItem, Func<object?, IPropertyType?, SyncSerializerOptions, Task<object?>> GetValueMethod, SyncSerializerOptions options, bool isImport)
    {
        if (blockItem == null) return;

        var contentType = await GetContentType(blockItem.ContentTypeKey);
        if (contentType is null) return;

        foreach (var value in blockItem.Values)
        {
            var property = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias == value.Alias);
            if (property == null) continue;

            var mappedValue = await GetValueMethod(value.Value, property, options);
            if (mappedValue != null)
                value.Value = mappedValue;

            // the property type is what populates editorAlias when the value is serialized.
            if (isImport) value.PropertyType = property;
        }

        // Umbraco sorts values by culture when it saves or publishes a block value, 
        // so we do the same, or the draft won't match the published value.
        if (isImport)
            blockItem.Values = [.. blockItem.Values.OrderBy(x => x.Culture, StringComparer.OrdinalIgnoreCase)];
    }

#pragma warning disable CS0618 // Type or member is obsolete (post v18, we will need to have our own model?)
    private bool MigrateBlock(BlockItemData? block)
    {
        if (block is null) return false;

        bool converted = false;
        if (block.Values.Count == 0 && block.RawPropertyValues?.Count > 0)
        {
            block.Values = SyncBlockMapperBase<TBlockValue>.MigrateBlockRawValues(block.RawPropertyValues);
            block.RawPropertyValues.Clear();
            converted = true;
        }

        // can no longer does this migration like this as Udi isn't set anymore. 

        //if (block.Key == Guid.Empty && block.Udi is GuidUdi guidUdi)
        //{
        //    block.Key = guidUdi.Guid;
        //    converted = true;
        //}

        //block.Udi = null;

        return converted;
    }
#pragma warning restore CS0618 // Type or member is obsolete

    private static List<BlockPropertyValue> MigrateBlockRawValues(Dictionary<string, object?> rawValues)
    {
        var values = new List<BlockPropertyValue>();
        foreach (var kvp in rawValues)
        {
            values.Add(new BlockPropertyValue
            {
                Alias = kvp.Key,
                Value = kvp.Value
            });
        }
        return values;
    }

    private async Task<IContentType?> GetContentType(Guid contentTypeKey)
        => await _contentTypeService.GetAsync(contentTypeKey);

    private static TBlockValue? GetBlockValue(string value)
    {
        if (value.TryDeserialize<TBlockValue>(out var blockValue) && blockValue is not null)
            return blockValue;

        return null;
    }

    public override async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
    {
        var stringValue = value.ToString();
        if (value is BlockPropertyValue blockPropertyValue)
        {
            stringValue = blockPropertyValue.Value?.ToString() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(stringValue)) return [];

        var blockValue = SyncBlockMapperBase<TBlockValue>.GetBlockValue(stringValue);
        if (blockValue == null) return [];

        var dependencies = new List<uSyncDependency>();

        List<BlockItemData> blocks = [
            ..blockValue.ContentData,
            ..blockValue.SettingsData
        ];

        foreach (var block in blocks)
        {
            dependencies.AddRange(await GetBlockDependencies(block, flags));
        }

        return dependencies;
    }

    private async Task<IEnumerable<uSyncDependency>> GetBlockDependencies(BlockItemData block, DependencyFlags flags)
    {
        var dependencies = new List<uSyncDependency>();
        var contentType = await GetContentType(block.ContentTypeKey);
        if (contentType is null) return dependencies;

        if (flags.HasFlag(DependencyFlags.IncludeDependencies))
        {
            // the content type for the block
            var contentTypeDependency = this.CreateDependency(contentType.GetUdi(), flags);
            dependencies.AddNotNull(contentTypeDependency);
        }

        foreach (var value in block.Values)
        {
            var property = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias == value.Alias);
            if (property == null) continue;

            if (value.Value is null) continue;
            dependencies.AddRange(await _mapperCollection.Value.GetDependenciesAsync(value.Value, property.PropertyEditorAlias, flags));
        }
        return dependencies;
    }
}
