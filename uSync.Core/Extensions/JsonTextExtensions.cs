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
    /// <summary>
    ///  will try and parse a string value into a JsonNode, 
    /// </summary>
    /// <remarks>
    ///  if the value isn't json then this will return false. 
    /// </remarks>
    public static bool TryParseJsonNode(this string value, out JsonNode? node)
    {
        node = default;
        if (value.DetectIsJson()) return false;

        try
        {
            node = JsonNode.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///  will attempt to turn the string value into a JsonNode
    /// </summary>
    /// <remarks>
    ///  unlike TryParseJsonNode() if the value isn't json, we will
    ///  attempt to make it a string json node. 
    /// </remarks>
    public static bool TryGetAsJsonNodeValue(this string value, [MaybeNullWhen(false)] out JsonNode? node)
    {
        if (value.TryParseJsonNode(out node))
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

    /// <summary>
    ///  will take a json object, that might have embedded json strings in values and turn it into a 
    ///  truly nested json object. 
    /// </summary>
    public static bool TryExpandJsonNodeValue(this JsonNode value, [MaybeNullWhen(false)] out JsonNode? node)
    {
        node = value;
        if (value == null) return false;

        switch(value.GetValueKind())
        {
            case JsonValueKind.String:
                return value.ToString().TryGetAsJsonNodeValue(out node);
            case JsonValueKind.Object:
                var jsonObject = value.AsObject();
                foreach(var property in jsonObject.ToList())
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
                for(int n = 0; n <=  jsonArray.Count; n++)  
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


    public static bool TryGetJsonNodeFromObject(this object value, [MaybeNullWhen(false)] out JsonNode? node)
    {
        node = default;

        if (value.TryGetValueAs<string>(out var stringValue) is false 
            || string.IsNullOrEmpty(stringValue)) return false; 

        return stringValue.TryGetAsJsonNodeValue(out node); 
    }

    private static bool TryGetValueAs<TObject>(this object value, [MaybeNullWhen(false)] out TObject? result)
    {
        result = default;
        if (value == null) return false;
        var attempt = value.TryConvertTo<TObject>();
        if (attempt is false || attempt.Result is null) return attempt;
        result = attempt.Result;
        return true;
    }

}
