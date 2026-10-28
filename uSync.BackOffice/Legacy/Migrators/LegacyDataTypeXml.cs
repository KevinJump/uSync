using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace uSync.BackOffice.Legacy.Migrators;

/// <summary>
///  helpers for reading and rewriting a data type's xml in a legacy file.
/// </summary>
internal static class LegacyDataTypeXml
{
    public static string GetEditorAlias(XElement dataType)
        => dataType.Element("Info")?.Element("EditorAlias")?.Value ?? string.Empty;

    public static Guid GetKey(XElement dataType)
        => Guid.TryParse(dataType.Attribute("Key")?.Value, out var key) ? key : Guid.Empty;

    public static JsonObject GetConfig(XElement dataType)
    {
        var json = dataType.Element("Config")?.Value;
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            return JsonNode.Parse(json) as JsonObject ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static void SetEditor(XElement dataType, string editorAlias, string editorUiAlias, JsonObject config)
    {
        var info = dataType.Element("Info");
        if (info is null) return;

        SetChild(info, "EditorAlias", editorAlias);
        SetChild(info, "EditorUIAlias", editorUiAlias);

        var configNode = dataType.Element("Config");
        if (configNode is null)
        {
            configNode = new XElement("Config");
            dataType.Add(configNode);
        }

        configNode.ReplaceNodes(new XCData(config.ToJsonString()));
    }

    private static void SetChild(XElement parent, string name, string value)
    {
        var child = parent.Element(name);
        if (child is null) parent.Add(new XElement(name, value));
        else child.Value = value;
    }

    /// <summary>
    ///  get a value from the config, ignoring the case of the key.
    /// </summary>
    public static JsonNode? GetValue(JsonObject config, string key)
    {
        foreach (var item in config)
        {
            if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
                return item.Value;
        }
        return null;
    }

    public static bool TryGetBoolean(JsonObject config, string key, out bool value)
    {
        value = false;
        var node = GetValue(config, key);
        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<bool>(out value)) return true;
            if (jsonValue.TryGetValue<string>(out var text) && bool.TryParse(text, out value)) return true;
        }
        return false;
    }

    public static int? GetInt(JsonNode? node)
    {
        if (node is not JsonValue value) return null;
        if (value.TryGetValue<int>(out var number)) return number;
        if (value.TryGetValue<string>(out var text) && int.TryParse(text, out number)) return number;
        return null;
    }
}
