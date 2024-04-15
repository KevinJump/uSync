using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using NUnit.Framework;

using Umbraco.Extensions;

using uSync.Core.DataTypes;
using uSync.Core.Extensions;

namespace uSync.Tests.Migrations;
internal class MigrationTestBase
{

	protected void TestSerializerPropertyMigration(IConfigurationSerializer serializer, string source, string target)
	{
		if (source.TryDeserialize(out IDictionary<string, object> dictionaryData) is false || dictionaryData is null)
			return;

		dictionaryData = dictionaryData.ConvertToCamelCase();

		var result = serializer.GetConfigurationImport(dictionaryData);

		var targetDictionary =
			JsonSerializer.Serialize(
				JsonSerializer.Deserialize<JsonObject>(target).ToDictionary(),
				JsonTextExtensions._defaultOptions
			);
		var resultJson = JsonSerializer.Serialize(result, JsonTextExtensions._defaultOptions);

		Assert.AreEqual(targetDictionary, resultJson);
	}
}
