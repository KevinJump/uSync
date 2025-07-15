using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Extensions;

using uSync.Core.Extensions;

namespace uSync.Core.DataTypes.DataTypeSerializers;
internal class RichTextEditorMigratingSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
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
        configuration = MigrateToTipTap(configuration);
        configuration = FixMediaParent(configuration);
        configuration = TopLevelEditor(configuration);
        return configuration.ToImmutableSortedDictionary();
    }

    private IDictionary<string, object> MigrateToTipTap(IDictionary<string, object> configuration)
    {
        if (_options.DisableMigration is true)
            return configuration;

        if (configuration.ContainsKey("mode") is false && configuration.ContainsKey("hideLabel") is false)
        {
            _logger.LogDebug("Skipping Tiptap migration as it does not contain 'mode' or 'hideLabel'.");   
            // if both mode and hideLabel are not present, then this has probibly already been migrated
            return configuration;
        }

        _logger.LogDebug("Migrating TinyMCE configuration to Tiptap format.");
        // do the tip tap things. 

        if (!configuration.TryGetValue("toolbar", out var toolbar) || toolbar is not List<string> toolBarList)
        {
            return configuration;
        }

        configuration.Remove("mode");
        configuration.Remove("hideLabel");

        var newToolbar = toolBarList.Select(MapToolbarItem).WhereNotNull().ToList();
        configuration["toolbar"] = new List<List<List<string>>> { new() { newToolbar } };

        var extensions = new List<string>
        {
            "Umb.Tiptap.RichTextEssentials",
            "Umb.Tiptap.Embed",
            "Umb.Tiptap.Figure",
            "Umb.Tiptap.Image",
            "Umb.Tiptap.Link",
            "Umb.Tiptap.MediaUpload",
            "Umb.Tiptap.Subscript",
            "Umb.Tiptap.Superscript",
            "Umb.Tiptap.Table",
            "Umb.Tiptap.TextAlign",
            "Umb.Tiptap.TextDirection",
            "Umb.Tiptap.TextIndent",
            "Umb.Tiptap.Underline"
        };

        if (configuration.TryGetValue("blocks", out var blocks) && blocks is not null)
        {
            // if there are blocks, we need to add the block extension
            extensions.Add("Umb.Tiptap.Block");
        }

        configuration["extensions"] = extensions.ToArray();

        return configuration;
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
            var json = value.Value.SerializeJsonString();
            if (json == null) continue;

            if (json.TryDeserialize<JsonElement>(out var node))
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
