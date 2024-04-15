using System.Collections.Immutable;
using System.Text.Json;

using Umbraco.Cms.Core;
using Umbraco.Extensions;

using uSync.Core.DataTypes;
using uSync.Core.Extensions;

namespace uSync.Core.Serialization.Serializers;
internal class RichTextEditorMigratingSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
	public string Name => nameof(RichTextEditorMigratingSerializer);

	public string[] Editors => [Constants.PropertyEditors.Aliases.RichText];

	public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
	{
		configuration = TopLevelEditor(configuration);
		configuration = FixMediaParent(configuration);

		return configuration.ToImmutableSortedDictionary();
	}

	private IDictionary<string, object> FixMediaParent(IDictionary<string, object> configuration)
	{
		if (configuration.TryGetValue("mediaParentId", out var mediaParent) is false || mediaParent is null)
			return configuration;

		if (mediaParent is JsonElement element is false || element.ValueKind != JsonValueKind.String)
			return configuration;

		var mediaParentKey = element.ToString();
		if (string.IsNullOrWhiteSpace(mediaParentKey)) return configuration;


		if (UdiParser.TryParse(mediaParentKey, out var mediaUdi) is false)
			return configuration;

		if (mediaUdi is GuidUdi guidUdi is false) 
			return configuration;

		var mediaItem = new MediaParentItem()
		{
			Key = $"_media_parent_{mediaParentKey}".EncodeAsGuid().ToString(),
			MediaKey = guidUdi.Guid.ToString()
		};

		configuration["mediaParentId"] = mediaItem.AsEnumerableOfOne();
		return configuration;
	}

	private class MediaParentItem
	{
		public required string Key { get; set; }
		public required string MediaKey { get; set; }
		public string MediaTypeAlias { get; set; } = "";
		public string? FocalPoint { get; set; } = null;
		public string[] Crops { get; set; } = [];
	}

	private IDictionary<string, object> TopLevelEditor(IDictionary<string, object> configuration)
	{
		if (configuration.TryGetValue("editor", out var editorObject) is false || editorObject is null)
			return configuration;

		if (editorObject is JsonElement element == false) return configuration;
		if (element.ValueKind != JsonValueKind.Object) return configuration;

		if (element.ToString().TryDeserialize<Dictionary<string, object>>(out var obj) is false || obj is null)
			return configuration;

		configuration.Remove("editor");

		foreach (var value in obj)
		{
			configuration[value.Key] = value.Value;
		}

		return configuration;
	}
}
