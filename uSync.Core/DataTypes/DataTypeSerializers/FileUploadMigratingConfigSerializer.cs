using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Umbraco.Cms.Core;

using uSync.Core.Extensions;

namespace uSync.Core.DataTypes.DataTypeSerializers;
internal class FileUploadMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
	public string Name => nameof(FileUploadMigratingConfigSerializer);

	public string[] Editors => [Constants.PropertyEditors.Aliases.UploadField];

	public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
	{
		if (configuration.TryGetValue("fileExtensions", out var items) is false || items is null)
			return configuration;

		if (items is JsonArray element == false) return configuration;

		if (element.ToString().TryDeserialize<List<IdValuePair>>(out var values) is false || values is null)
			return configuration;

		var convertedItems = new List<string>();

		foreach(var item in values.OrderBy(x => x.Id))
		{
			if (item.Value is null) continue;
			convertedItems.Add(item.Value);
		}

		configuration["fileExtensions"] = convertedItems;
		return configuration;
	}

	private class IdValuePair
	{
		public int? Id { get; set; }
		public string? Value { get; set; }
	}

}
