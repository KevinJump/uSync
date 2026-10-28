using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core;

namespace uSync.Core.DataTypes.DataTypeSerializers;

/// <summary>
///  v19 moves the per-area minAllowed/maxAllowed in the block grid to a single validation limit.
/// </summary>
/// <remarks>
///  area limits are already optional, so (unlike the other editors) a zero is kept as a real bound.
/// </remarks>
internal class BlockGridMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    private const string _minAllowedKey = "minAllowed";
    private const string _maxAllowedKey = "maxAllowed";

    public string Name => nameof(BlockGridMigratingConfigSerializer);
    public string[] Editors => [Constants.PropertyEditors.Aliases.BlockGrid];

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        var blocksKey = configuration.FindKey("blocks");
        if (blocksKey is null) return configuration;

        if (ToJsonArray(configuration[blocksKey]) is not JsonArray blocks) return configuration;

        var changed = false;
        foreach (var area in blocks.OfType<JsonObject>()
            .Select(x => x["areas"]).OfType<JsonArray>()
            .SelectMany(x => x.OfType<JsonObject>()))
        {
            changed |= MigrateArea(area);
        }

        if (changed is false) return configuration;

        return new SortedDictionary<string, object>(configuration, StringComparer.OrdinalIgnoreCase)
        {
            [blocksKey] = blocks
        };
    }

    private static bool MigrateArea(JsonObject area)
    {
        if (area.ContainsKey(_minAllowedKey) is false && area.ContainsKey(_maxAllowedKey) is false)
            return false;

        var min = ConfigurationRangeExtensions.ToBound(area[_minAllowedKey], zeroIsUnbounded: false);
        var max = ConfigurationRangeExtensions.ToBound(area[_maxAllowedKey], zeroIsUnbounded: false);

        area.Remove(_minAllowedKey);
        area.Remove(_maxAllowedKey);
        area.Remove("validationLimit");

        var range = ConfigurationRangeExtensions.ToRange(min, max);
        if (range is not null) area["validationLimit"] = range;

        return true;
    }

    private static JsonArray? ToJsonArray(object? value)
        => value switch
        {
            JsonArray array => array,
            JsonElement { ValueKind: JsonValueKind.Array } element => JsonSerializer.SerializeToNode(element) as JsonArray,
            string json => JsonNode.Parse(json) as JsonArray,
            _ => null,
        };
}
