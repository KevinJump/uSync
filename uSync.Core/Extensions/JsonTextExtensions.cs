using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Extensions;

namespace uSync.Core.Extensions;

/// <summary>
///  extensions for System.Text.Json manipulation
/// </summary>
public static class JsonTextExtensions
{
    private static JsonSerializerOptions _defaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static JsonNodeOptions _nodeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    #region JsonNode 

    /// <summary>
    ///  is the string valid json. 
    /// </summary>
    public static bool IsValidJsonString(this string? value)
        => value.TryParseToJsonNode(out _);

    /// <summary>
    ///  will try and parse a string value into a JsonNode, 
    /// </summary>
    /// <remarks>
    ///  if the value isn't json then this will return false. 
    /// </remarks>
    public static bool TryParseToJsonNode(this string? value, [MaybeNullWhen(false)] out JsonNode node)
    {
        node = default;
        if (string.IsNullOrEmpty(value) || value.DetectIsJson() is false) return false;

        try
        {
            node = JsonNode.Parse(value, _nodeOptions);
            return true;
        }
        catch
        {
            return false;
        }
    }


    /// <summary>
    ///  try to get a json representation of an object, 
    /// </summary>
    public static bool TryParseToJsonNode(this object value, [MaybeNullWhen(false)] out JsonNode node)
    {
        node = default;

        if (value.TryGetValueAs<string>(out var stringValue) is false
            || stringValue == null) return false;

        return stringValue.TryParseToJsonNode(out node);
    }


    public static JsonNode? ToJsonNode(this string? value)
        => TryParseToJsonNode(value, out JsonNode? node) ? node : default;

    public static bool TrySerializeJsonNode(this JsonNode node, [MaybeNullWhen(false)] out string result,
        bool indent = true)
    {
        try
        {
            result = node.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = indent,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static string SerializeJsonNode(this JsonNode node, bool indent = true)
    {
        node.TrySerializeJsonNode(out var jsonString, indent);
        return jsonString ?? node.ToJsonString();
    }


    /// <summary>
    ///  will attempt to turn the string value into a JsonNode
    /// </summary>
    /// <remarks>
    ///  unlike TryParseJsonNode() if the value isn't json, we will
    ///  attempt to make it a string json node. 
    /// </remarks>
    public static bool TryConvertToJsonNode(this string value, [MaybeNullWhen(false)] out JsonNode? node)
    {
        if (value.TryParseToJsonNode(out node))
            return true;

        // else - didn't parse , we can try as a string.
        try
        {
            node = JsonNode.Parse($"\"{value}\"");
            return true;
        }
        catch
        {
            return default;
        }
    }

    public static bool TryConvertToJsonNode(this object value, [MaybeNullWhen(false)] out JsonNode node)
    {
        node = default;

        if (value.TryGetValueAs<string>(out var stringValue) is false
            || stringValue == null) return false;

        return stringValue.TryConvertToJsonNode(out node);
    }


    public static JsonNode? ConvertToJsonNode(this string value)
        => value.TryConvertToJsonNode(out var node) ? node : default;

    public static JsonNode? ConvertToJsonNode(this object value)
        => TryConvertToJsonNode(value, out JsonNode? node) ? node : default;


    #endregion

    #region JsonObject 

    public static bool TryParseToJsonObject(this string? value, [MaybeNullWhen(false)] out JsonObject node)
    {
        node = default;
        if (value.TryParseToJsonNode(out var jsonNode) is false) return false;
        if (jsonNode.GetValueKind() != JsonValueKind.Object) return false;

        node = jsonNode.AsObject();
        if (node == null) return false;
        return true;
    }

    public static JsonObject? ToJsonObject(this string? value)
        => value.TryParseToJsonObject(out var jsonObject) ? jsonObject : default;  

    public static bool TryConvertToJsonObject(this object value, [MaybeNullWhen(false)] out JsonObject result)
    {
        result = default;
        if (value.TryConvertToJsonNode(out var node) is false || node is null)
            return false;

        try
        {
            result = node.AsObject();
        }
        catch
        {

        }

        return result != default;
    }

    public static JsonObject? ConvertToJsonObject(this object value)
        => value.TryConvertToJsonObject(out JsonObject? result) ? result : default;

    #endregion

    #region JsonArray

    public static bool TryParseToJsonArray(this string? value, [MaybeNullWhen(false)] out JsonArray node)
    {
        node = default;
        if (value.TryParseToJsonNode(out var jsonNode) is false || jsonNode is null) return false;
        if (jsonNode.GetValueKind() != JsonValueKind.Array) return false;

        node = jsonNode.AsArray();
        if (node == null) return false;
        return true;
    }

    /// <summary>
    ///  convert a string to a json array 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static JsonArray? ToJsonArray(this string? value)
        => value.TryParseToJsonArray(out var jsonArray) ? jsonArray : default;


    /// <summary>
    ///  enumerates a JsonArray as a list of JsonObjects
    /// </summary>
    public static IEnumerable<JsonObject?> AsListOfJsonObjects(this JsonArray array)
        => array.Select(x => x?.AsObject());

    #endregion

    #region JsonExpansion 

    /// <summary>
    ///  will fully expand any json elements inside any json string. 
    /// </summary>
    public static JsonNode ExpandAllJsonInToken(this JsonNode node)
        => TryExpandJsonNodeValue(node, out var jsonNode) ? jsonNode ?? node : node;

    /// <summary>
    ///  will take a json object, that might have embedded json strings in values and turn it into a 
    ///  truly nested json object. 
    /// </summary>
    public static bool TryExpandJsonNodeValue(this JsonNode value, [MaybeNullWhen(false)] out JsonNode node)
    {
        node = value;
        if (value == null) return false;

        switch (value.GetValueKind())
        {
            case JsonValueKind.String:
                return value.ToString().TryConvertToJsonNode(out node);
            case JsonValueKind.Object:
                var jsonObject = value.AsObject();
                foreach (var property in jsonObject.ToList())
                {
                    if (property.Value?.TryExpandJsonNodeValue(out var innerNode) is true)
                    {
                        jsonObject[property.Key] = innerNode;
                    }
                }
                node = jsonObject;
                return true;
            case JsonValueKind.Array:
                var jsonArray = value.AsArray();
                for (int n = 0; n <= jsonArray.Count; n++)
                {
                    if (jsonArray[n]?.TryExpandJsonNodeValue(out var innerNode) is true)
                    {
                        jsonArray[n] = innerNode;
                    }
                }
                node = jsonArray;
                return true;
            default:
                return true;
        }
    }

    /// <summary>
    ///  convert a string value into a fully expanded JsonNode object 
    /// </summary>
    public static JsonNode? ConvertStringToExpandedJson(this string value)
    {
        // try parse this into json (if its a string we make a string jsonnode)
        if (value.TryConvertToJsonNode(out var jsonNode) is false || jsonNode == null)
            return default;


        // expand the json to within an inch of its life.
        if (jsonNode.TryExpandJsonNodeValue(out var expandedJson) is false || expandedJson is null)
            return jsonNode;

        return expandedJson;
    }

    /// <summary>
    ///  takes a string of mixed json, explodes and encoded json and returns it as a string.
    /// </summary>
    public static string ConvertStringToExpandedJsonString(this string value, bool indented = true)
    {
        var json = value.ConvertStringToExpandedJson();
        if (json == null) return value;

        return json.SerializeJsonNode(indented);
    }


    #endregion

    #region serialize / deserialzie 
    public static bool TryDeserialize<TObject>(this string? value, [MaybeNull] out TObject result)
    {
        result = default;

        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            result = JsonSerializer.Deserialize<TObject>(value, _defaultOptions);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryDeserialize(this string value, Type type, [MaybeNull] out object result)
    {
        try
        {
            result = JsonSerializer.Deserialize(value, type, _defaultOptions);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static object? Deserialize(this string value, Type type)
        => JsonSerializer.Deserialize(value, type, _defaultOptions);

    public static TObject? Deserialize<TObject>(this string value)
        => JsonSerializer.Deserialize<TObject>(value, _defaultOptions);

    public static bool TrySerialize(this object value, [MaybeNull] out string result)
    {
        try
        {
            result = JsonSerializer.Serialize(value, _defaultOptions);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static string Serialize(this object value, bool indent = true)
        => JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = indent,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

    private static bool TryGetValueAs<TObject>(this object value, [MaybeNullWhen(false)] out TObject result)
    {
        result = default;
        if (value == null) return false;
        var attempt = value.TryConvertTo<TObject>();
        if (attempt is false || attempt.Result is null) return attempt;
        result = attempt.Result;
        return true;
    }

    #endregion

    #region property getters 

    /// <summary>
    ///  attempt to find a property on a JsonObject and return it as JsonObject
    /// </summary>
    public static bool TryGetPropertyAsObject(this JsonObject jsonObject, string propertyName, [MaybeNullWhen(false)] out JsonObject result)
    {
        result = default;

        if (jsonObject.TryGetPropertyValue(propertyName, out var propertyNode) is false || propertyNode is null)
            return false;

        try { 
            result = propertyNode.AsObject();
        }
        catch
        {
            return false;
        }
        return result != default;
    }

    public static string GetPropertyAsString(this JsonObject obj, string propertyName)
    {
        if (obj.TryGetPropertyValue(propertyName, out var value))
            return value?.ToString() ?? string.Empty;

        return string.Empty;
    }

    public static bool TryGetPropertyAsArray(this JsonObject jsonObject, string propertyName, [MaybeNullWhen(false)] out JsonArray result)
    {
        result = default;

        if (jsonObject.TryGetPropertyValue(propertyName, out var propertyNode) is false || propertyNode is null)
            return false;

        if (propertyNode.GetValueKind() != JsonValueKind.Array)
            return false;

        try
        {
            result = propertyNode.AsArray();
        }
        catch
        {
            return false;
        }

        return result != default;
    }


    public static JsonArray GetPropertyAsArray(this JsonObject obj, string propertyName)
    {
        if (obj.TryGetPropertyAsArray(propertyName, out var value))
            return value;

        return new JsonArray();
    }

    public static JsonObject? GetPropertyAsObject(this JsonObject obj, string propertyName)
    {
        if (obj.TryGetPropertyAsObject(propertyName, out var value))
            return value;

        return default;
    }
    #endregion

}
