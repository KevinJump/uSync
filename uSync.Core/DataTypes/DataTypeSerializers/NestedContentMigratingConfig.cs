using System.Text.Json.Nodes;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;

using uSync.Core.Extensions;

namespace uSync.Core.DataTypes.DataTypeSerializers;


/// <summary>
///  migrates nested content to a block list element. 
/// </summary>

internal class NestedContentMigratingConfig : ConfigurationSerializerBase, IConfigurationSerializer
{
    private readonly IContentTypeService _contentTypeService;

    public NestedContentMigratingConfig(IContentTypeService contentTypeService)
    {
        this._contentTypeService = contentTypeService;
    }

    public string Name => nameof(NestedContentMigratingConfig);
    public string[] Editors => [SyncLegacyTypes.NestedContent, SyncLegacyTypes.OurNestedContent];

    public string? GetEditorAlias() => Constants.PropertyEditors.Aliases.BlockList;
    public string? GetEditorUIAlias() => "Umb.PropertyEditorUi.BlockList";

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        var config = new BlockListConfiguration();

        if (configuration.TryGetValue("minItems", out var min) && int.TryParse(min?.ToString(), out var minItems))
            config.ValidationLimit.Min = minItems;

        if (configuration.TryGetValue("maxItems", out var max) && int.TryParse(max?.ToString(), out var maxItems))
            config.ValidationLimit.Max = maxItems;

        if (configuration.TryGetValue("contentTypes", out var contentTypes) && contentTypes is JsonArray contentTypesArray)
        {
            var blocks = new List<BlockListConfiguration.BlockConfiguration>();
            foreach (var contentType in contentTypesArray.Cast<JsonObject>())
            {
                if (contentType["ncAlias"]?.ToString() is not string alias) continue;
                var contentTypeItem = _contentTypeService.Get(alias);
                if (contentTypeItem is null) continue;
                blocks.Add(new BlockListConfiguration.BlockConfiguration
                {
                    ContentElementTypeKey = contentTypeItem.Key,
                });
            }

            config.Blocks = [.. blocks];
        }

        var result = config.SerializeJsonString().DeserializeJson<Dictionary<string, object>>() ?? configuration;
        return result;
    }
}
