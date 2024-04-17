using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core;
using Umbraco.Extensions;

using uSync.Core.Extensions;

namespace uSync.Core.DataTypes.DataTypeSerializers;
internal class RichTextEditorMigratingSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
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

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        configuration = FixMediaParent(configuration);
        configuration = TopLevelEditor(configuration);
        return configuration.ToImmutableSortedDictionary();
    }

	private IDictionary<string, object> FixMediaParent(IDictionary<string, object> configuration)
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

    private IDictionary<string, object> TopLevelEditor(IDictionary<string, object> configuration)
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
