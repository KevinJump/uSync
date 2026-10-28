using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace uSync.Core.DataTypes;

/// <summary>
///  helpers for the Umbraco v19 config changes, where editors with separate min/max
///  values moved to a single range value (e.g. min/max => validationRange: { min, max }).
/// </summary>
/// <remarks>
///  these mirror Umbraco's own V_19_0_0 MigrateMinMaxToRange migrations, so a file
///  from an earlier version imports the same way an upgraded site would migrate.
/// </remarks>
internal static class ConfigurationRangeExtensions
{
    /// <summary>
    ///  move separate min/max keys into a single range object.
    /// </summary>
    /// <remarks>
    ///  if neither key is present (e.g. the file is already in the v19 format) the
    ///  configuration is returned unchanged.
    /// </remarks>
    public static IDictionary<string, object> MigrateMinMaxToRange(
        this IDictionary<string, object> configuration,
        string minKey,
        string maxKey,
        string rangeKey,
        bool minZeroIsUnbounded = true,
        bool maxZeroIsUnbounded = true)
    {
        var minName = configuration.FindKey(minKey);
        var maxName = configuration.FindKey(maxKey);
        if (minName is null && maxName is null) return configuration;

        var result = configuration.ToMutable();

        var min = minName is not null ? ToBound(result[minName], minZeroIsUnbounded) : null;
        var max = maxName is not null ? ToBound(result[maxName], maxZeroIsUnbounded) : null;

        if (minName is not null) result.Remove(minName);
        if (maxName is not null) result.Remove(maxName);

        var rangeName = result.FindKey(rangeKey);
        if (rangeName is not null) result.Remove(rangeName);

        var range = ToRange(min, max);
        if (range is not null) result[rangeKey] = range;

        return result;
    }

    /// <summary>
    ///  remove keys (case insensitive) from the configuration.
    /// </summary>
    public static IDictionary<string, object> RemoveKeys(this IDictionary<string, object> configuration, params string[] keys)
    {
        if (keys.All(x => configuration.FindKey(x) is null)) return configuration;

        var result = configuration.ToMutable();
        foreach (var key in keys)
        {
            var name = result.FindKey(key);
            if (name is not null) result.Remove(name);
        }
        return result;
    }

    /// <summary>
    ///  get a boolean config value, only if it is actually set in the configuration.
    /// </summary>
    public static bool TryGetBoolean(this IDictionary<string, object> configuration, string key, out bool value)
    {
        value = false;
        var name = configuration.FindKey(key);
        if (name is null) return false;

        bool? parsed = configuration[name] switch
        {
            bool b => b,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            JsonValue jsonValue when jsonValue.TryGetValue<bool>(out var b) => b,
            var other => bool.TryParse(GetString(other), out var b) ? b : null,
        };

        value = parsed ?? false;
        return parsed.HasValue;
    }

    /// <summary>
    ///  get a numeric config value, only if it is actually set in the configuration.
    /// </summary>
    public static bool TryGetDecimal(this IDictionary<string, object> configuration, string key, out decimal value)
    {
        value = 0;
        var name = configuration.FindKey(key);
        if (name is null) return false;

        var result = ToDecimal(configuration[name]);
        value = result ?? 0;
        return result.HasValue;
    }

    /// <summary>
    ///  get a string config value, only if it is actually set in the configuration.
    /// </summary>
    public static bool TryGetString(this IDictionary<string, object> configuration, string key, out string value)
    {
        value = string.Empty;
        var name = configuration.FindKey(key);
        if (name is null) return false;

        value = GetString(configuration[name]) ?? string.Empty;
        return string.IsNullOrWhiteSpace(value) is false;
    }

    public static string? FindKey(this IDictionary<string, object> configuration, string key)
        => configuration.Keys.FirstOrDefault(x => x.Equals(key, StringComparison.OrdinalIgnoreCase));

    public static JsonObject? ToRange(decimal? min, decimal? max)
    {
        if (min is null && max is null) return null;

        var range = new JsonObject();
        if (min is not null) range["min"] = JsonValue.Create(min.Value);
        if (max is not null) range["max"] = JsonValue.Create(max.Value);
        return range;
    }

    public static decimal? ToBound(object? value, bool zeroIsUnbounded)
    {
        var bound = ToDecimal(value);
        if (bound is null) return null;
        return zeroIsUnbounded && bound.Value == 0m ? null : bound;
    }

    private static decimal? ToDecimal(object? value)
        => value switch
        {
            null => null,
            decimal d => d,
            int i => i,
            long l => l,
            double d => (decimal)d,
            JsonElement { ValueKind: JsonValueKind.Number } element when element.TryGetDecimal(out var d) => d,
            JsonValue jsonValue when jsonValue.TryGetValue<decimal>(out var d) => d,
            var other => decimal.TryParse(GetString(other), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null,
        };

    private static string? GetString(object? value)
        => value switch
        {
            null => null,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
            JsonValue jsonValue when jsonValue.TryGetValue<string>(out var s) => s,
            _ => value.ToString(),
        };

    /// <summary>
    ///  some serializers return immutable dictionaries, so we copy before changing anything.
    /// </summary>
    private static IDictionary<string, object> ToMutable(this IDictionary<string, object> configuration)
        => new SortedDictionary<string, object>(configuration, StringComparer.OrdinalIgnoreCase);
}
