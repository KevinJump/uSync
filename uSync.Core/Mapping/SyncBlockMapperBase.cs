using Microsoft.Extensions.Logging;

using System.Collections;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Extensions;
using uSync.Core.Mapping.Mappers;

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

    public override async Task<string?> GetImportValueAsync(string value, string editorAlias)
        => await ProcessBlockValuesAsync(value, GetImportProperty);

    public override async Task<string?> GetExportValueAsync(object value, string editorAlias)
        => await ProcessBlockValuesAsync(value?.ToString() ?? string.Empty, GetExportProperty);

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

    private async Task<object?> GetImportProperty(object? value, string propertyEditorAlias)
    {
        if (_mapperCollection.Value is null) return value;
        _logger.LogDebug("Importing block value for {PropertyEditorAlias} {valueType}", propertyEditorAlias, value?.GetType().Name ?? "blank");
        var importString = SyncBlockMapperBase<TBlockValue>.GetStringValue(value) ?? string.Empty;
        return await _mapperCollection.Value.GetImportValueAsync(importString, propertyEditorAlias);
    }

    private async Task<object?> GetExportProperty(object? value, string propertyEditorAlias)
    {
        if (_mapperCollection.Value is null) return value;
        _logger.LogDebug("Exporting block value for {PropertyEditorAlias} {valueType}", propertyEditorAlias, value?.GetType().Name ?? "blank");
        var exportValueAsString = SyncBlockMapperBase<TBlockValue>.GetStringValue(value) ?? string.Empty;
        var result = await _mapperCollection.Value.GetExportValueAsync(exportValueAsString, propertyEditorAlias);
        return result.ConvertToJsonNode()?.ExpandAllJsonInToken() ?? result;
    }

    private async Task<string?> ProcessBlockValuesAsync(string value, Func<object?, string, Task<object?>> GetValueMethod)
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

            await ProcessBlockData(contentItem, GetValueMethod);
        }

        if (blockValue.Expose.Count == 0)
        {
            // migration from v14 to v15+ block values.
            blockValue.Expose = [.. blockValue.ContentData.Select(x => new BlockItemVariation(x.Key, null, null))];
        }

        return blockValue.SerializeJsonString(true);
    }

    private async Task ProcessBlockData(BlockItemData? blockItem, Func<object?, string, Task<object?>> GetValueMethod)
    {
        if (blockItem == null) return;

        var contentType = await GetContentType(blockItem.ContentTypeKey);
        if (contentType is null) return;

        foreach (var value in blockItem.Values)
        {
            var property = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias == value.Alias);
            if (property == null) continue;

            var mappedValue = await GetValueMethod(value.Value, property.PropertyEditorAlias);
            if (mappedValue != null)
                value.Value = mappedValue;
        }
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

        if (block.Key == Guid.Empty && block.Udi is GuidUdi guidUdi)
        {
            block.Key = guidUdi.Guid;
            converted = true;
        }

        block.Udi = null;

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
