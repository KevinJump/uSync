using System.Text.Json;

using Umbraco.Cms.Core;

using uSync.Core.Extensions;

namespace uSync.Core.DataTypes.DataTypeSerializers;

internal class DataListMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
	public string Name => nameof(DataListMigratingConfigSerializer);
	public string[] Editors => [
		Constants.PropertyEditors.Aliases.CheckBoxList,
		Constants.PropertyEditors.Aliases.DropDownListFlexible,
		Constants.PropertyEditors.Aliases.RadioButtonList
	];

	public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
	{
		if (configuration.TryGetValue("items", out var items) is false || items is null)
			return configuration;

		if (items is JsonElement element == false) return configuration;
		if (element.ValueKind != JsonValueKind.Array) return configuration;

		if (element.ToString().TryDeserialize<List<IdValuePair>>(out var values) is false || values is null)
			return configuration;

		var convertedItems = new List<string>();

		foreach(var item in values.OrderBy(x => x.Id))
		{
			if (item?.Value is null) continue;
			convertedItems.Add(item.Value);
		}

		configuration["items"] = convertedItems;

		return configuration;
	}
	private class IdValuePair
	{
		public int? Id { get; set; }
		public string? Value { get; set; }
	}

}
