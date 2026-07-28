using Jumoo.Json;

using NUnit.Framework;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Extensions;

using uSync.Core.DataTypes;

using static uSync.Core.Extensions.DictionaryExtensions;

namespace uSync.Tests.Migrations;
internal class MigrationTestBase
{

    protected void TestSerializerPropertyMigration(IConfigurationSerializer serializer, string source, string target)
    {
        if (source.TryDeserialize(out IDictionary<string, object> dictionaryData) is false || dictionaryData is null)
            return;

        dictionaryData = dictionaryData.ConvertToCamelCase();

        var result = serializer.GetConfigurationImportAsync("test", dictionaryData).Result;

        var targetDictionary =
            JsonSerializer.Serialize(
                JsonSerializer.Deserialize<JsonObject>(target).ToDictionary(),
                JsonTextOptions.GetOptions()
            );
        var resultJson = JsonSerializer.Serialize(result, JsonTextOptions.GetOptions());

        Assert.That(resultJson, Is.EqualTo(targetDictionary));
    }
}
