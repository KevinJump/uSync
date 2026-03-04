using System;
using System.Collections.Generic;
using System.Text;

using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Services;

using uSync.Core.Extensions;

namespace uSync.Core.Mapping.Mappers;

/// <summary>
///  helper methods that can convert nested content to block list values. 
/// </summary>
internal class NestedContentToBlockListHelper
{
    private readonly IContentTypeService _contentTypeService;

    public NestedContentToBlockListHelper(IContentTypeService contentTypeService)
    {
        _contentTypeService = contentTypeService;
    }

    private static string[] _reservedProperties = ["ncContentTypeAlias", "key", "name"];

    /// <summary>
    ///  converts a nested content value to a block list value. This is used when we are converting a nested content property to a block list property.
    /// </summary>
    /// <param name="nestedContentValue">the nested content value to convert</param>
    /// <returns>the converted block list value</returns>
    public string ConvertNestedContentToBlockList(string nestedContentValue)
    {
        if (nestedContentValue.Contains("ncContentTypeAlias") is false) return nestedContentValue;

        List<Dictionary<string, object>>? nestedContent;
        try
        {
            nestedContent = nestedContentValue.DeserializeJson<List<Dictionary<string, object>>>();
        }
        catch (Exception)
        {
            // If deserialization fails (e.g. malformed or partially corrupted JSON),
            // fall back to returning the original value.
            return nestedContentValue;
        }
        if (nestedContent == null) return nestedContentValue;

        BlockListValue blockListValue = new BlockListValue();

        foreach (var item in nestedContent)
        {
            var contentTypeAlias = item.TryGetValue("ncContentTypeAlias", out var alias) ? alias?.ToString() : null;
            if (contentTypeAlias == null) continue;

            var contentType = _contentTypeService.Get(contentTypeAlias);
            if (contentType == null) continue;

            var blockItemData = new BlockItemData
            {
                ContentTypeKey = contentType.Key,
                Key = item.TryGetValue("key", out var key) && Guid.TryParse(key?.ToString(), out var guidKey) ? guidKey : Guid.NewGuid(),
            };

            foreach (var value in item.Keys)
            {
                if (_reservedProperties.Contains(value)) continue;
                blockItemData.Values.Add(new BlockPropertyValue
                {
                    Alias = value,
                    Value = item[value]
                });
            }

            blockListValue.ContentData.Add(blockItemData);
        }

        blockListValue.Expose = [.. blockListValue.ContentData.Select(x => new BlockItemVariation(x.Key, null, null))];
        blockListValue.Layout = new Dictionary<string, IEnumerable<IBlockLayoutItem>>
        {
            {
                "Umbraco.BlockList",
                blockListValue.ContentData.Select(
                x => new BlockListLayoutItem
                {
                    ContentKey = x.Key,
                    SettingsKey = null,
                })
            }
        };

        return blockListValue.SerializeJsonString() ?? nestedContentValue;
    }
}
