using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace uSync.BackOffice.Legacy.Migrators;

/// <summary>
///  moves the (deprecated) Multi Node Tree Picker onto the dedicated content, media and member pickers.
/// </summary>
internal class MultiNodeTreePickerLegacyMigrator : ISyncLegacyMigrator
{
    private const string _mntpAlias = "Umbraco.MultiNodeTreePicker";

    private const string _contentPicker = "Umbraco.ContentPicker";
    private const string _documentPickerMultiple = "Umbraco.DocumentPicker.Multiple";
    private const string _mediaPickerSingle = "Umbraco.MediaPicker.Single";
    private const string _mediaPicker = "Umbraco.MediaPicker3";
    private const string _memberPicker = "Umbraco.MemberPicker";
    private const string _memberPickerMultiple = "Umbraco.MemberPicker.Multiple";

    private static readonly Dictionary<string, string> _editorUis = new()
    {
        [_contentPicker] = "Umb.PropertyEditorUi.DocumentPicker",
        [_documentPickerMultiple] = "Umb.PropertyEditorUi.DocumentPicker.Multiple",
        [_mediaPickerSingle] = "Umb.PropertyEditorUi.MediaPicker.Single",
        [_mediaPicker] = "Umb.PropertyEditorUi.MediaPicker",
        [_memberPicker] = "Umb.PropertyEditorUi.MemberPicker",
        [_memberPickerMultiple] = "Umb.PropertyEditorUi.MemberPicker.Multiple",
    };

    public string Id => "multiNodeTreePicker";
    public string Name => "Multi Node Tree Pickers";
    public string Description
        => "Multi Node Tree Pickers move to the Document, Media or Member picker that matches the tree they pick from.";

    public string? Notice
        => "The editor changes, so templates and models will see a different model type. " +
           "Media values are converted in content, media and member files; values inside blocks and rich text are not.";

    public bool MigratesValues => true;

    public bool AppliesTo(XElement dataType, int folderMajorVersion)
        => LegacyDataTypeXml.GetEditorAlias(dataType).Equals(_mntpAlias, StringComparison.OrdinalIgnoreCase);

    public SyncLegacyDataTypeMigration? MigrateDataType(XElement dataType)
    {
        var config = LegacyDataTypeXml.GetConfig(dataType);

        var treeSource = GetTreeSource(config);
        var treeType = (treeSource?["type"]?.ToString() ?? "content").ToLowerInvariant();
        if (treeType is not ("content" or "media" or "member"))
            return null;

        var limit = GetLimit(config);
        var single = limit.Max == 1;

        var target = (treeType, single) switch
        {
            ("content", true) => _contentPicker,
            ("content", false) => _documentPickerMultiple,
            ("media", true) => _mediaPickerSingle,
            ("media", false) => _mediaPicker,
            ("member", true) => _memberPicker,
            _ => _memberPickerMultiple,
        };

        var newConfig = new JsonObject();

        if (treeType != "member")
        {
            var startNode = GetStartNodeKey(treeSource);
            if (startNode is not null) newConfig["startNodeId"] = startNode;

            if (treeType == "content" && treeSource?["dynamicRoot"] is JsonNode dynamicRoot)
                newConfig["dynamicRoot"] = dynamicRoot.DeepClone();

            var filter = LegacyDataTypeXml.GetValue(config, "filter")?.ToString();
            if (string.IsNullOrWhiteSpace(filter) is false && filter.Equals("null", StringComparison.OrdinalIgnoreCase) is false)
                newConfig[treeType == "content" ? "allowedContentTypes" : "filter"] = filter;

            if (LegacyDataTypeXml.TryGetBoolean(config, "ignoreUserStartNodes", out var ignore))
                newConfig["ignoreUserStartNodes"] = ignore;

            // single pickers have no count to validate, and 0 means unbounded.
            if (single is false && (limit.Min > 0 || limit.Max > 0))
            {
                var range = new JsonObject();
                if (limit.Min > 0) range["min"] = limit.Min;
                if (limit.Max > 0) range["max"] = limit.Max;
                newConfig["validationLimit"] = range;
            }
        }

        LegacyDataTypeXml.SetEditor(dataType, target, _editorUis[target], newConfig);

        return new SyncLegacyDataTypeMigration(LegacyDataTypeXml.GetKey(dataType), _mntpAlias, target, this);
    }

    /// <summary>
    ///  the picked items are stored as a comma separated list of udis.
    /// </summary>
    /// <remarks>
    ///  content and member pickers store the same thing, but the media pickers store a json array of items.
    /// </remarks>
    public string MigrateValue(string value, SyncLegacyDataTypeMigration migration)
    {
        if (migration.NewEditorAlias is not (_mediaPicker or _mediaPickerSingle))
            return value;

        if (string.IsNullOrWhiteSpace(value) || value.TrimStart().StartsWith('[')) return value;

        var items = new JsonArray();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (TryGetKey(part, out var mediaKey) is false) continue;

            items.Add(new JsonObject
            {
                ["key"] = Guid.NewGuid().ToString(),
                ["mediaKey"] = mediaKey.ToString(),
                ["mediaTypeAlias"] = string.Empty,
                ["crops"] = new JsonArray(),
                ["focalPoint"] = null,
            });
        }

        return items.Count == 0 ? string.Empty : items.ToJsonString();
    }

    private static JsonObject? GetTreeSource(JsonObject config)
    {
        var node = LegacyDataTypeXml.GetValue(config, "treeSource");
        if (node is JsonObject obj) return obj;

        if (node is JsonValue value && value.TryGetValue<string>(out var text) && string.IsNullOrWhiteSpace(text) is false)
        {
            try { return JsonNode.Parse(text) as JsonObject; }
            catch (JsonException) { }
        }

        return null;
    }

    private static string? GetStartNodeKey(JsonObject? treeSource)
    {
        var id = treeSource?["id"]?.ToString();
        if (string.IsNullOrWhiteSpace(id)) return null;

        return TryGetKey(id, out var key) ? key.ToString() : null;
    }

    private static (int Min, int Max) GetLimit(JsonObject config)
    {
        // a file might already have the new range shape.
        if (LegacyDataTypeXml.GetValue(config, "validationLimit") is JsonObject limit)
            return (LegacyDataTypeXml.GetInt(limit["min"]) ?? 0, LegacyDataTypeXml.GetInt(limit["max"]) ?? 0);

        return (LegacyDataTypeXml.GetInt(LegacyDataTypeXml.GetValue(config, "minNumber")) ?? 0,
            LegacyDataTypeXml.GetInt(LegacyDataTypeXml.GetValue(config, "maxNumber")) ?? 0);
    }

    /// <summary>
    ///  get the key from a udi (umb://media/{guid}) or a plain guid.
    /// </summary>
    private static bool TryGetKey(string value, out Guid key)
    {
        var index = value.LastIndexOf('/');
        return Guid.TryParse(index >= 0 ? value[(index + 1)..] : value, out key);
    }
}
