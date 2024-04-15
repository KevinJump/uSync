using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Strings;

using uSync.Core.Extensions;

using static Umbraco.Cms.Core.PropertyEditors.ColorPickerConfiguration;

namespace uSync.Core.DataTypes.DataTypeSerializers;

internal class ColourPickerMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
	public string Name => nameof(ColourPickerMigratingConfigSerializer);
	public string[] Editors => [Constants.PropertyEditors.Aliases.ColorPicker];
	public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
	{
		if (configuration.TryGetValue("items", out var items) is false || items is null)
			return configuration;

		if (items is JsonElement element == false) return configuration;
		if (element.ValueKind != JsonValueKind.Array) return configuration;

		var convertedItems = new List<ColorPickerItem>();

		var array = element.EnumerateArray().Select(x => x);
		
		foreach(var item in element.EnumerateArray())
		{
			if (item.ValueKind != JsonValueKind.Object) continue;
			var obj = JsonSerializer.Deserialize<JsonObject>(item.ToString());
			if (obj == null) continue;

			var valueJson = obj.GetPropertyAsString("value");
			if (string.IsNullOrEmpty(valueJson)) continue;

			if (valueJson.TryDeserialize<JsonObject>(out var itemValues) is false || itemValues	is null) 
				return configuration;

			convertedItems.Add(new ColorPickerItem
			{
				Label = itemValues.GetPropertyAsString("label"),
				Value = itemValues.GetPropertyAsString("value")
			});
		}

		configuration.Remove("items");
		configuration.Add("items", convertedItems);

		return configuration;
	}
}