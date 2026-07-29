using Jumoo.Json;

using J2N.Collections.ObjectModel;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Extensions;

namespace uSync.Core.DataTypes.DataTypeSerializers;

internal class RichTextEditorMigratingSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    private static string[] _defaultToolbar = ["sourcecode", "bold", "italic", "underline", "alignleft", "aligncenter", "alignright",
                          "bullist", "numlist", "outdent", "indent", "link", "umbmediapicker", "umbembeddialog"];

    private static string[] _defaultExtensions = [
        "Umb.Tiptap.RichTextEssentials", "Umb.Tiptap.Anchor", "Umb.Tiptap.Blockquote", "Umb.Tiptap.Bold", "Umb.Tiptap.BulletList",
        "Umb.Tiptap.CodeBlock", "Umb.Tiptap.Embed", "Umb.Tiptap.Figure", "Umb.Tiptap.Heading", "Umb.Tiptap.HorizontalRule",
        "Umb.Tiptap.HtmlAttributeClass", "Umb.Tiptap.HtmlAttributeDataset", "Umb.Tiptap.HtmlAttributeId", "Umb.Tiptap.HtmlAttributeStyle",
        "Umb.Tiptap.HtmlTagDiv", "Umb.Tiptap.HtmlTagSpan", "Umb.Tiptap.Image", "Umb.Tiptap.Italic", "Umb.Tiptap.Link",
        "Umb.Tiptap.MediaUpload", "Umb.Tiptap.OrderedList", "Umb.Tiptap.Strike", "Umb.Tiptap.Subscript", "Umb.Tiptap.Superscript",
        "Umb.Tiptap.Table", "Umb.Tiptap.TextAlign", "Umb.Tiptap.TextDirection", "Umb.Tiptap.TextIndent", "Umb.Tiptap.TrailingNode",
        "Umb.Tiptap.Underline", "Umb.Tiptap.WordCount"];

    private readonly TinyMceToTiptapMigrationSettings _options;
    private readonly ILogger<RichTextEditorMigratingSerializer> _logger;

    public RichTextEditorMigratingSerializer(IOptions<TinyMceToTiptapMigrationSettings> options, ILogger<RichTextEditorMigratingSerializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string Name => nameof(RichTextEditorMigratingSerializer);

    public string[] Editors => [
        "Umbraco.TinyMCE",
        Constants.PropertyEditors.Aliases.RichText
    ];

    /// <summary>
    ///  we migrate this one, from "Umbraco.TinyMCE" to "Umbraco.RichText" 
    /// </summary>
    /// <returns></returns>
    public string? GetEditorAlias()
        => Constants.PropertyEditors.Aliases.RichText;

    /// <summary>
    ///  updates the editor UI alias to the new Tiptap one, if migration is enabled.
    /// </summary>
    public string? GetEditorUIAlias()
    {
        if (_options.DisableMigration is true) return null;
        return "Umb.PropertyEditorUi.Tiptap";
    }

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        configuration = FixMediaParent(configuration);
        configuration = TopLevelEditor(configuration);
        configuration = MigrateToTipTap(configuration);
        return configuration.ToImmutableSortedDictionary();
    }

    private IDictionary<string, object> MigrateToTipTap(IDictionary<string, object> configuration)
    {
        if (_options?.DisableMigration is true)
            return configuration;

        if (configuration.ContainsKey("extensions") is true)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Skipping Tiptap migration as it already contains 'extensions'.");

            return configuration;
        }

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Migrating TinyMCE configuration to Tiptap format.");

        // do the tip tap things. 

        // update we don't skip if the toolbar is missing, i can be for some older configs
        List<string> toolbarList = [];
        if (configuration.TryGetValue("toolbar", out var toolbar) is false
            || TryGetToolbarArray(toolbar, out toolbarList) is false)
        {
            toolbarList = [.. _defaultToolbar];
        }

        if (toolbarList.Count == 0) 
            toolbarList = [.. _defaultToolbar];

        configuration.Remove("mode");
        configuration.Remove("hideLabel");

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Original toolbar items: {ToolbarItems}", string.Join(", ", toolbarList));

        var newToolbar = toolbarList.Select(MapToolbarItem).WhereNotNull().ToList();

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Mapped toolbar items: {ToolbarItems}", string.Join(", ", newToolbar));

        var extensions = _defaultExtensions.ToList();
        if (configuration.TryGetValue("blocks", out var blocks) && blocks is not null)
        {
            // if there are blocks, we need to add the block extension
            newToolbar.Add("Umb.Tiptap.Toolbar.BlockPicker");
            extensions.Add("Umb.Tiptap.Block");
        }
        
        if (configuration.ContainsKey("toolbar"))
            configuration.Remove("toolbar");

        configuration["toolbar"] = new List<List<List<string>>> { new() { newToolbar } };
        
        configuration["extensions"] = extensions.ToArray();

        return configuration;
    }

    private bool TryGetToolbarArray(object? toolbar, out List<string> toolBarList)
    {
        toolBarList = new List<string>();
        if (toolbar is null) return false;

        if (toolbar is IEnumerable<string> stringList)
        {
            toolBarList = stringList.ToList();
            return toolBarList.Count > 0;
        }

        if (toolbar is not JsonElement jsonElement || jsonElement.ValueKind != JsonValueKind.Array)
            return false;

        toolBarList = jsonElement.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        return true;
    }

    private string? MapToolbarItem(string item)
    {
        return item switch
        {
            "undo" => "Umb.Tiptap.Toolbar.Undo",
            "redo" => "Umb.Tiptap.Toolbar.Redo",
            "cut" => null,
            "copy" => null,
            "paste" => null,
            "styles" => "Umb.Tiptap.Toolbar.StyleSelect",
            "fontname" => "Umb.Tiptap.Toolbar.FontFamily",
            "fontfamily" => "Umb.Tiptap.Toolbar.FontFamily",
            "fontsize" => "Umb.Tiptap.Toolbar.FontSize",
            "forecolor" => "Umb.Tiptap.Toolbar.TextColorForeground",
            "backcolor" => "Umb.Tiptap.Toolbar.TextColorBackground",
            "blockquote" => "Umb.Tiptap.Toolbar.Blockquote",
            "formatblock" => null,
            "removeformat" => "Umb.Tiptap.Toolbar.ClearFormatting",
            "bold" => "Umb.Tiptap.Toolbar.Bold",
            "italic" => "Umb.Tiptap.Toolbar.Italic",
            "underline" => "Umb.Tiptap.Toolbar.Underline",
            "strikethrough" => "Umb.Tiptap.Toolbar.Strike",
            "alignleft" => "Umb.Tiptap.Toolbar.TextAlignLeft",
            "aligncenter" => "Umb.Tiptap.Toolbar.TextAlignCenter",
            "alignright" => "Umb.Tiptap.Toolbar.TextAlignRight",
            "alignjustify" => "Umb.Tiptap.Toolbar.TextAlignJustify",
            "bullist" => "Umb.Tiptap.Toolbar.BulletList",
            "numlist" => "Umb.Tiptap.Toolbar.OrderedList",
            "outdent" => "Umb.Tiptap.Toolbar.TextOutdent",
            "indent" => "Umb.Tiptap.Toolbar.TextIndent",
            "anchor" => "Umb.Tiptap.Toolbar.Anchor",
            "table" => "Umb.Tiptap.Toolbar.Table",
            "hr" => "Umb.Tiptap.Toolbar.HorizontalRule",
            "subscript" => "Umb.Tiptap.Toolbar.Subscript",
            "superscript" => "Umb.Tiptap.Toolbar.Superscript",
            "charmap" => "Umb.Tiptap.Toolbar.CharacterMap",
            "rtl" => "Umb.Tiptap.Toolbar.TextDirectionRtl",
            "ltr" => "Umb.Tiptap.Toolbar.TextDirectionLtr",
            "link" => "Umb.Tiptap.Toolbar.Link",
            "unlink" => "Umb.Tiptap.Toolbar.Unlink",
            "sourcecode" => "Umb.Tiptap.Toolbar.SourceEditor",
            "umbmediapicker" => "Umb.Tiptap.Toolbar.MediaPicker",
            "umbembeddialog" => "Umb.Tiptap.Toolbar.EmbeddedMedia",
            "umbblockpicker" => "Umb.Tiptap.Toolbar.BlockPicker",
            _ => null
        };
    }

    private static IDictionary<string, object> FixMediaParent(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue("mediaParentId", out var mediaParent) is false || mediaParent is null)
            return configuration;

        if (mediaParent is string mediaParentKey is false)
            return configuration;

        if (string.IsNullOrWhiteSpace(mediaParentKey)) return configuration;

        if (UdiParser.TryParse(mediaParentKey, out var mediaUdi) is false)
            return configuration;

        if (mediaUdi is GuidUdi guidUdi is false)
            return configuration;

        configuration["mediaParentId"] = guidUdi.Guid;
        return configuration;
    }

    private static IDictionary<string, object> TopLevelEditor(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue("editor", out var editorObject) is false || editorObject is null)
            return configuration;

        if (editorObject is JsonObject element == false) return configuration;

        if (element.ToString().TryDeserialize<Dictionary<string, object>>(out var obj) is false || obj is null)
            return configuration;

        configuration.Remove("editor");

        foreach (var value in obj)
        {
            // anything that doesn't round-trip through json - a null, or a bare scalar - is
            // already the value we want, so it goes across as-is.
            if (value.Value.SerializeJsonString() is string json
                && json.TryDeserialize<JsonElement>(out var node))
            {
                configuration[value.Key] = node;
            }
            else
            {
                configuration[value.Key] = value.Value;
            }
        }

        return configuration;
    }
}
