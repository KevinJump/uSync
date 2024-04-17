using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.PropertyEditors;
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

		if (items is JsonArray element == false) return configuration;

		var convertedItems = new List<ColorPickerItem>();

	
		foreach(var item in element)
		{
			var obj = item?.ConvertToJsonObject();
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

		// Colour picker the labels are upper case in the value
		// var json = JsonSerializer.Serialize(convertedItems);
		var json = convertedItems.SerializeJsonString();
		if (json != null)
		{
			var itemsValue = json.ToJsonNode();
			if (itemsValue != null)
			{
				configuration.Remove("items");
				configuration.Add("items", itemsValue);
			}
		}

		return configuration;
	}
}