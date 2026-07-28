using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

using Jumoo.Json;

namespace uSync.Core.Extensions;

/// <summary>
///  extensions for System.Text.Json manipulation
/// </summary>
/// <remarks>
/// <para>
///  These have moved to the <c>Jumoo.Json</c> package, so there is one implementation shared
///  across the Jumoo packages. What is left here forwards to it, and keeps the exact behaviour
///  these methods had before the move - including the places where <c>Jumoo.Json</c> returns
///  null and this returned <c>string.Empty</c>.
/// </para>
/// <para>
///  To move over, replace <c>using uSync.Core.Extensions;</c> with <c>using Jumoo.Json;</c>.
///  Both cannot be in scope in the same file: they declare the same extension method
///  signatures, so a call that matches both is ambiguous (CS0121).
/// </para>
/// </remarks>
[Obsolete("Use the equivalent extension in Jumoo.Json - will be removed in v20")]
public static class JsonTextExtensions
{
    #region JsonNode

    /// <summary>
    ///  is the string valid json.
    /// </summary>
    public static bool IsValidJsonString(this string? value)
        => JsonNodeExtensions.IsValidJsonString(value);

    /// <summary>
    ///  will try and parse a string value into a JsonNode,
    /// </summary>
    /// <remarks>
    ///  if the value isn't json then this will return false.
    /// </remarks>
    public static bool TryParseToJsonNode(this string? value, [MaybeNullWhen(false)] out JsonNode node)
        => JsonNodeExtensions.TryParseToJsonNode(value, out node);

    /// <summary>
    ///  try to get a json representation of an object,
    /// </summary>
    public static bool TryParseToJsonNode(this object value, [MaybeNullWhen(false)] out JsonNode node)
        => JsonNodeExtensions.TryParseToJsonNode(value, out node);

    public static JsonNode? ToJsonNode(this string? value)
        => JsonNodeExtensions.ToJsonNode(value);

    public static bool TrySerializeJsonNode(this JsonNode node, [MaybeNullWhen(false)] out string result,
        bool indent = true)
        => JsonNodeExtensions.TrySerializeJsonNode(node, out result, indent);

    public static string SerializeJsonNode(this JsonNode node, bool indent = true)
        => JsonSerialization.SerializeJsonNode(node, indent) ?? node.ToJsonString();

    /// <summary>
    ///  will attempt to turn the string value into a JsonNode
    /// </summary>
    /// <remarks>
    ///  unlike TryParseJsonNode() if the value isn't json, we will
    ///  attempt to make it a string json node.
    /// </remarks>
    public static bool TryConvertToJsonNode(this string value, [MaybeNullWhen(false)] out JsonNode? node)
        => JsonNodeExtensions.TryConvertToJsonNode(value, out node);

    public static bool TryConvertToJsonNode(this object value, [MaybeNullWhen(false)] out JsonNode node)
        => JsonNodeExtensions.TryConvertToJsonNode(value, out node);

    public static JsonNode? ConvertToJsonNode(this string value)
        => JsonNodeExtensions.ConvertToJsonNode(value);

    public static JsonNode? ConvertToJsonNode(this object value)
        => JsonNodeExtensions.ConvertToJsonNode(value);

    #endregion

    #region JsonObject

    public static bool TryParseToJsonObject(this string? value, [MaybeNullWhen(false)] out JsonObject node)
        => JsonObjectExtensions.TryParseToJsonObject(value, out node);

    public static JsonObject? ToJsonObject(this string? value)
        => JsonObjectExtensions.ToJsonObject(value);

    public static bool TryConvertToJsonObject(this object value, [MaybeNullWhen(false)] out JsonObject result)
        => JsonObjectExtensions.TryConvertToJsonObject(value, out result);

    public static JsonObject? ConvertToJsonObject(this object value)
        => JsonObjectExtensions.ConvertToJsonObject(value);

    public static void AddOrRemoveIfNull<T>(this JsonObject? jsonObject, string property, T? value)
        where T : JsonNode
        => JsonObjectExtensions.AddOrRemoveIfNull(jsonObject, property, value);

    #endregion

    #region JsonArray

    public static bool TryParseToJsonArray(this string? value, [MaybeNullWhen(false)] out JsonArray node)
        => JsonArrayExtensions.TryParseToJsonArray(value, out node);

    /// <summary>
    ///  convert a string to a json array
    /// </summary>
    public static JsonArray? ToJsonArray(this string? value)
        => JsonArrayExtensions.TryParseToJsonArray(value, out var jsonArray) ? jsonArray : default;

    /// <summary>
    ///  enumerates a JsonArray as a list of JsonObjects
    /// </summary>
    public static IEnumerable<JsonObject?> AsListOfJsonObjects(this JsonArray array)
        => JsonArrayExtensions.AsListOfJsonObjects(array);

    #endregion

    #region JsonExpansion

    /// <summary>
    ///  will fully expand any json elements inside any json string.
    /// </summary>
    public static JsonNode ExpandAllJsonInToken(this JsonNode node)
        => JsonExpansions.ExpandAllJsonInToken(node);

    /// <summary>
    ///  will take a json object, that might have embedded json strings in values and turn it into a
    ///  truly nested json object.
    /// </summary>
    public static bool TryExpandJsonNodeValue(this JsonNode value, [MaybeNullWhen(false)] out JsonNode node)
        => JsonExpansions.TryExpandJsonNodeValue(value, out node);

    /// <summary>
    ///  convert a string value into a fully expanded JsonNode object
    /// </summary>
    public static JsonNode? ConvertStringToExpandedJson(this string value)
        => JsonExpansions.ConvertStringToExpandedJson(value);

    /// <summary>
    ///  takes a string of mixed json, explodes and encoded json and returns it as a string.
    /// </summary>
    public static string ConvertStringToExpandedJsonString(this string value, bool indented = true)
        => JsonExpansions.ConvertStringToExpandedJsonString(value, indented);

    #endregion

    #region serialize / deserialzie

    public static bool TryDeserialize<TObject>(this string? value, [MaybeNull] out TObject result)
        => JsonSerialization.TryDeserialize(value, out result);

    public static bool TryDeserialize(this string value, Type type, [MaybeNull] out object result)
        => JsonSerialization.TryDeserialize(value, type, out result);

    /// <remarks>
    ///  the Jumoo.Json version returns the default value where this used to throw.
    /// </remarks>
    public static object? DeserializeJson(this string value, Type type)
        => JsonSerialization.DeserializeJson(value, type);

    /// <remarks>
    ///  the Jumoo.Json version returns the default value where this used to throw.
    /// </remarks>
    public static TObject? DeserializeJson<TObject>(this string value)
        => JsonSerialization.DeserializeJson<TObject>(value);

    public static bool TrySerializeJsonString(this object value, [MaybeNull] out string result)
        => JsonSerialization.TrySerializeJsonString(value, out result);

    public static string SerializeJsonString(this object value, bool indent = true)
        => value is null ? string.Empty : JsonSerialization.SerializeJsonString(value, indent) ?? string.Empty;

    /// <summary>
    ///  Convert a value to the requested type.
    /// </summary>
    public static bool TryGetValueAs<TObject>(this object? value, [MaybeNullWhen(false)] out TObject result)
        => JsonSerialization.TryGetValueAs(value, out result);

    /// <summary>
    ///  Convert a value to the requested runtime type.
    /// </summary>
    public static bool TryGetValueAs(this object? value, Type targetType, [MaybeNullWhen(false)] out object result)
        => JsonSerialization.TryGetValueAs(value, targetType, out result);

    #endregion

    #region property getters

    /// <summary>
    ///  attempt to find a property on a JsonObject and return it as JsonObject
    /// </summary>
    public static bool TryGetPropertyAsObject(this JsonObject jsonObject, string propertyName, [MaybeNullWhen(false)] out JsonObject result)
        => JsonPropertyExtensions.TryGetPropertyAsJsonObject(jsonObject, propertyName, out result);

    /// <summary>
    ///  Gets the json property as a string, or returns string.empty
    /// </summary>
    public static string GetPropertyAsString(this JsonObject obj, string propertyName)
        => JsonPropertyExtensions.GetPropertyAsString(obj, propertyName) ?? string.Empty;

    public static bool GetPropertyAsBool(this JsonObject obj, string propertyName, bool defaultValue)
        => JsonPropertyExtensions.GetPropertyAsBool(obj, propertyName, defaultValue);

    public static TResult GetPropertyValueOrDefault<TResult>(this JsonObject obj, string propertyName, TResult defaultValue)
        => JsonPropertyExtensions.GetPropertyValueOrDefault(obj, propertyName, defaultValue);

    public static bool TryGetPropertyAsArray(this JsonObject jsonObject, string propertyName, [MaybeNullWhen(false)] out JsonArray result)
        => JsonPropertyExtensions.TryGetPropertyAsArray(jsonObject, propertyName, out result);

    public static JsonArray GetPropertyAsArray(this JsonObject obj, string propertyName)
        => JsonPropertyExtensions.GetPropertyAsArray(obj, propertyName);

    public static JsonObject? GetPropertyAsObject(this JsonObject obj, string propertyName)
        => JsonPropertyExtensions.GetPropertyAsJsonObject(obj, propertyName);

    #endregion

    #region Comparasions

    /// <summary>
    ///  tells us if the json for an object is equal, helps when the config objects don't have their
    ///  own Equals functions
    /// </summary>
    public static bool IsJsonEqual(this object? currentObject, object? newObject)
        => JsonComparisons.IsJsonEqual(currentObject, newObject);

    #endregion

    #region Type Checks

    /// <summary>
    ///  checks if the value is a non-string JSON value (array, object, number, boolean).
    /// </summary>
    public static bool IsNonStringJsonValue(this object? value)
        => JsonNodeExtensions.IsNonStringJsonValue(value);

    #endregion

}
